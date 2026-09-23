## Context

The app is an ASP.NET Core MVC (net10.0) budget app using EF Core + PostgreSQL, Pico CSS, htmx, Alpine, and lucide icons. Transactions are per-user (Rabobank CSV import), the overview (`Features/Budget/OverviewController`, `Views/Overview/Index.cshtml`) renders transaction rows per week and per IBAN with an htmx partial-swap pattern (`Features/Transactions/ToggleFixedController` + `Views/Shared/_ToggleFixed.cshtml` shows the `AntiForgeryToken` + `hx-post` + `hx-swap="outerHTML"` + `hx-swap-oob` idiom). Test mode authenticates via the `X-Test-User` header and bypasses antiforgery. See proposal.md for motivation and the `merchant-logos` spec for the behavioral contract.

## Goals / Non-Goals

**Goals:**
- A global, lazily-populated merchant map holding only *decisions* (logo URL or explicit no-logo), never written by CSV import.
- Overview transaction rows render a circular logo for mapped names and a placeholder otherwise, with graceful fallback on image load failure.
- An htmx-driven admin page (search + sortable columns + map/link/no-logo/clear actions) that lists every distinct counterparty name seen in any user's transactions.
- Deterministic e2e coverage using test-owned merchant names and fixed logo URLs (no live external services).

**Non-Goals:**
- Any logo discovery or suggestion feature (logosear.ch, favicon services, image search). The admin page only accepts manually entered logo URLs; there is no lookup or suggestion in the app.
- Automatic alias suggestions or fuzzy matching. Linking a name to a merchant is always an explicit user action; the normalizer never guesses that two names belong together.
- Logo upload/hosting. URLs are stored as given and loaded by the browser.

## Decisions

### 1. Merchant map is lazy — the tables store only decisions
Neither table has a row until the user maps a name, marks it no-logo, or links it to a merchant. The admin page derives the full name list from transactions (`SELECT DISTINCT NameOtherPartyNormalized`), left-joined to `Merchant` and `MerchantAlias` by normalized key. Unknown merchants fall back to placeholder.

- **Why:** CSV import stays simple (writes only transactions), and the table never accumulates hundreds of placeholder rows. This is the "unmapped queue" by construction.
- **Alternative rejected:** eager rows created at import — simpler admin page but writes junk to a shared table on every import.

### 2. Entities: canonical merchant + alias links
```
Merchant
 ├── Id               int     (surrogate primary key)
 ├── NameNormalized   string  (unique, canonical key)
 ├── DisplayName      string? (canonical name shown on the overview when present)
 ├── LogoUrl          string? (null when Status == None)
 ├── Status           enum { Mapped, None }
 └── UpdatedAt        DateTimeOffset

MerchantAlias
 ├── Id               int     (surrogate primary key)
 ├── NameNormalized   string  (unique; a folded-in name that differs from the canonical key)
 ├── MerchantId       int     (FK → Merchant; links always point to a canonical merchant)
 └── CreatedAt        DateTimeOffset
```
`Id` is the primary key in both; `NameNormalized` is a unique, case-insensitive index (a unique constraint, not the key) in both. One shared table set, no `UserId` — global by spec. The `Transaction` entity additionally gains a `NameOtherPartyNormalized` column (see decision 3). A `MerchantNameNormalizer` static helper (trim, invariant lower, collapse whitespace runs, normalize hyphen spacing) is the single shared implementation: it computes `NameOtherPartyNormalized` at import, writes merchant/alias keys from the admin actions, and runs the backfill. Extracted as pure functions for unit testing. Links never chain (an alias always points to a `Merchant`, never to another alias).

### 3. Overview resolution is a single relational join
Each transaction carries `NameOtherPartyNormalized`, written once at CSV import by the one shared C# normalizer. The overview resolves merchants in a single query that left-joins the map: `Transactions LEFT JOIN Merchant m ON m.NameNormalized = t.NameOtherPartyNormalized LEFT JOIN MerchantAlias a ON a.NameNormalized = t.NameOtherPartyNormalized LEFT JOIN Merchant ma ON ma.Id = a.MerchantId`, coalescing `m`/`ma` as the resolved merchant. `LogoUrl`/`DisplayName` are threaded through the existing `TransactionTemplateModel`/view model as nullable fields, and the view shows `DisplayName` when present, otherwise the raw counterparty name. The join is backed by a non-unique index on `Transaction.NameOtherPartyNormalized`; no in-memory dictionaries or per-row queries.

### 4. Placeholder lives in the browser, not the server
Each row renders a fixed-size circular `.logo` container. If `LogoUrl` is present, an `<img src="...">` fills it and hides itself via `onerror` (e.g. `onerror="this.style.display='none'"`), leaving the CSS placeholder (muted circle with a lucide `wallet` icon) visible underneath. This satisfies "broken logo falls back to placeholder" without any server-side image fetching or URL validation beyond a format check.

### 5. Admin page: one full page + htmx list partial
- `GET /merchants` — full page: search input + table with sortable column headers.
- `GET /merchants/rows?search=&sort=&dir=` — htmx partial returning just the `<tbody>`. The search input and column headers use `hx-get`/`hx-target`/`hx-trigger` (debounced for typing), matching the existing htmx idiom.
- Sort columns: name, transaction count, first-seen date, status. Default: unmapped-first (neither `Merchant` nor `MerchantAlias` row), then linked, then no-logo, then mapped; within the unmapped group, ordered by transaction count descending so the most impactful names surface first, with a stable secondary sort by name.
- The name list query groups transactions by `NameOtherPartyNormalized`, computing count and earliest `Date` (`first-seen`), left-joined with `Merchant` and `MerchantAlias`. The transaction count is rendered as a dedicated `#txn` column.
- Each row's status is one of: unmapped, mapped, no-logo, or linked to `<merchant>`. Rows in the mapped/no-logo/linked states are resolved, so only unmapped rows form the mapping queue.
- Each row's inline map form captures the canonical display name and the logo URL together; the displayed name in the list is the display name when present, otherwise the raw name.
- Unmapped rows offer a "link to merchant" picker as an alternative to mapping. Expanding it shows an options panel: a search box plus the canonical merchants (both `Mapped` and `None`) ordered by total transaction count descending (counts folded across aliases), capped at ~10 initially, narrowed as the user types via `GET /merchants/options?q=`. Clicking an option submits `POST /merchants/link` and the row re-renders as "linked to `<merchant>`".
- Empty search state: when no canonical merchant matches, the panel offers "create as a new merchant", which switches the row to its map form with the display name prefilled from the raw counterparty name — routing to `POST /merchants/map` instead of a dead end. So link and map are two exits from the same decision: fold into an existing merchant, or promote the name to a merchant of its own.

### 6. Mapping actions: four POSTs returning the updated list partial
- `POST /merchants/map` — body `{ name, displayName, logoUrl }`. Validates the URL is an absolute http/https URI (format check only), then upserts `Mapped` with the display name and URL.
- `POST /merchants/link` — body `{ name, merchantName }`. Upserts a `MerchantAlias` from `normalize(name)` to the canonical `Merchant` whose key is `normalize(merchantName)`; rejected when the target merchant does not exist or the name is already a canonical merchant.
- `POST /merchants/none` — body `{ name }`. Upserts `None`.
- `POST /merchants/clear` — body `{ name }`. If the name is a canonical merchant, deletes it together with any aliases pointing at it (cascade); if the name is an alias, deletes just the alias.
All use the same antiforgery + htmx form pattern as `_ToggleFixed`, return the `rows` partial (with the search/sort state echoed so the swap is seamless), and preserve current search/sort. Names travel as form fields, not route segments, to avoid encoding issues (they contain spaces). Test mode bypasses antiforgery; production requires the token as with existing forms.

### 7. E2e strategy: test-owned names, fixed URLs, full-flow
Each test uses a GUID-suffixed merchant name (`jumbo-{guid}`) so no test ever collides with another on the shared map; per-user transaction isolation keeps other tests' overviews clean. Logo URLs are fixed (`https://example.com/logo.png`) so nothing touches the network. One test drives the whole feature: upload CSV with two GUID-suffixed names → open `/merchants` → search for the first name → map it with a display name and fixed logo URL → link the second name to it → open overview → assert the logo and display name render for both names; a third GUID name asserts the placeholder. New page objects (`MerchantsPage`) follow the existing `Pages/` + `Routes.cs` + `PlaywrightTestBase` structure.

## Risks / Trade-offs

- [The global map is shared mutable state across e2e tests] → Tests only touch GUID-owned names and assert by search/filter.
- [Broken/hotlinked logo URLs degrade the overview] → `onerror` placeholder fallback means a bad URL never breaks a row; URL format is validated at write time.
- [Normalization is a pure string rule; real-world names can differ in unexpected ways] → `NameOtherPartyNormalized` is written once at import by the single shared normalizer and `NameOtherParty` never changes after import, so the column cannot drift; the admin page shows the exact normalized key and status for every name, unmapped-first sorting surfaces anything that failed to resolve, and explicit links let the user fold variants together.
- [Linking a name is exact-match and manual] → The user decides what belongs together; the `#txn` column shows each name's weight so a mistaken link is easy to spot and clear.
- [Deleting a merchant cascades to its aliases] → Clearing a canonical merchant removes its links too, per spec; the admin page shows linked names so the blast radius is visible before clearing.
- [`SELECT DISTINCT ... GROUP BY` over all transactions per admin-page request] → Data volume is small (personal app, hundreds of names); mitigated by htmx partial updates (no full reloads) and database-side grouping.
- [External logo URLs leak user's merchant history to third parties via the browser] → Accepted: logos are loaded directly by the browser; mitigation is loading only URLs the user entered explicitly.

## Migration Plan

1. Add `Merchant` and `MerchantAlias` entities, add `NameOtherPartyNormalized` to `Transaction`, register in the DbContext, and create the EF migration (new tables + new column; no changes to existing columns).
2. Backfill existing transactions' `NameOtherPartyNormalized` with an idempotent startup pass that runs the real normalizer over rows whose column is empty (safe: the input never changes).
3. Compute `NameOtherPartyNormalized` for every imported transaction in the CSV importer.
4. Add the overview logo and display-name rendering behind the relational join (merchants, then aliases).
5. Add the admin page (full page + rows partial) and the four POST actions.
6. Unit tests (normalizer, importer, admin query, action handlers) and e2e tests (full-flow + placeholder).
7. Rollback: revert the migration — the merchant tables and the new column are additive and nothing existing depends on them.

## Open Questions

- None that would change the specs or approach. Exact admin-page styling (column layout, iconography) is left to implementation.
