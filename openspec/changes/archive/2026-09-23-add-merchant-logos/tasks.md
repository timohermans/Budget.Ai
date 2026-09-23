## 1. Data model

- [x] 1.1 Add `Merchant` entity (`Id` primary key, unique `NameNormalized`, `DisplayName?`, `LogoUrl?`, `Status` enum `Mapped|None`, `UpdatedAt`) and `MerchantAlias` entity (`Id` primary key, unique `NameNormalized`, `MerchantId` FK) in `src/Budget.Web/Domain/Merchants/`
- [x] 1.2 Add `NameOtherPartyNormalized` property to the `Transaction` entity
- [x] 1.3 Add `MerchantNameNormalizer` static helper (trim, invariant lowercase, collapse internal whitespace runs, normalize hyphen spacing) with pure functions
- [x] 1.4 Register `DbSet<Merchant>` and `DbSet<MerchantAlias>` in `BudgetDbContext` with `Id` primary keys, unique case-insensitive indexes on `NameNormalized`, the alias-to-merchant FK, and a non-unique index on `Transaction.NameOtherPartyNormalized`
- [x] 1.5 Set `NameOtherPartyNormalized` on every imported transaction in `RabobankCsvImporter` using the shared normalizer
- [x] 1.6 Add EF migration for the new tables and the `NameOtherPartyNormalized` column, plus an idempotent startup backfill that fills the column for existing transactions; verify `dotnet ef database update` applies it

## 2. Overview logo display

- [x] 2.1 In the overview controller, resolve merchants in a single relational join on `Transaction.NameOtherPartyNormalized`: left-join `Merchant` by key, left-join `MerchantAlias` by key, and coalesce to the resolved merchant
- [x] 2.2 Thread the nullable `LogoUrl` and `DisplayName` through the transaction template/view model used by the week and IBAN sections
- [x] 2.3 Render the circular logo cell in `Views/Overview/Index.cshtml` for both transaction lists: `<img>` when mapped, CSS placeholder (muted circle + lucide `wallet` icon) otherwise, `onerror` hiding a broken image so the placeholder shows through; show `DisplayName` when present, otherwise the raw counterparty name
- [x] 2.4 Add `.logo` CSS (fixed size, `border-radius: 50%`, placeholder styling) scoped to the overview stylesheet

## 3. Merchant admin page

- [x] 3.1 Add `MerchantsController` with `GET /merchants` (full page) and `GET /merchants/rows` (rows partial) endpoints
- [x] 3.2 Implement the name-list query: group transactions by `NameOtherPartyNormalized` with count and first-seen (`MIN(Date)`), left-join `Merchant` and `MerchantAlias`, default unmapped-first ordered by transaction count descending (most first) with stable name secondary sort; render the count in a dedicated `#txn` column and a status of unmapped / mapped / no-logo / linked to `<merchant>`
- [x] 3.3 Add `POST /merchants/map` to `MerchantsController`: creates a new canonical merchant (or updates an existing one) from the display name + logo URL, upserting `Mapped`, and returns the updated rows partial with search/sort preserved
- [x] 3.4 Add `POST /merchants/link` (fold a name into an existing merchant via alias upsert), `POST /merchants/none` (mark a name no-logo), and `POST /merchants/clear` (remove an alias, or a canonical merchant with its aliases) to `MerchantsController`, each returning the updated rows partial with search/sort preserved
- [x] 3.5 Validate `logoUrl` as an absolute http/https URI on `POST /merchants/map`; reject invalid values without modifying the merchant entry
- [x] 3.6 Add htmx wiring for the search input (debounced `hx-get` on `/merchants/rows`) and sortable column headers, echoing search/sort in the partial URLs
- [x] 3.7 Add the row action forms (map / link / none / clear) using the existing antiforgery + `hx-post` + `hx-swap` pattern, each posting to its endpoint and swapping in the returned rows partial
- [x] 3.8 Add `GET /merchants/options?q=` to `MerchantsController` returning canonical merchants (both `Mapped` and `None`) with display name (or key), ordered by total transaction count descending (counts folded across aliases), capped at ~10, filtered by `q`
- [x] 3.9 Add the link picker UI: per unmapped row, an expandable panel with a search box + option list (debounced `hx-get` on `/merchants/options`); clicking an option posts `POST /merchants/link` and swaps the row to its linked state
- [x] 3.10 Add the link picker's empty state: when no merchant matches, offer "create as a new merchant", switching the row to its map form with the display name prefilled from the raw counterparty name

## 4. Tests

- [x] 4.1 Unit tests: `MerchantNameNormalizer` (case, whitespace, collapse runs, hyphen spacing), importer writes `NameOtherPartyNormalized`, and the admin query (grouping, count, first-seen, unmapped-first ordering with transaction count descending, linked status)
- [x] 4.2 Unit tests: mapping actions (upsert `Mapped` with display name + URL, upsert `None`, link upsert, unlink on clear, cascade delete of a merchant's aliases, invalid URL rejected, update of existing mapping)
- [x] 4.3 Unit tests: overview resolution resolves an exact merchant, then an alias, then falls back to placeholder
- [x] 4.4 E2e: add `Routes.Merchants` and a `MerchantsPage` page object following the existing `Pages/` pattern
- [x] 4.5 E2e full-flow test: upload CSV with two GUID-suffixed names → search for the first on `/merchants` → map it with a display name and fixed `https://example.com/logo.png` URL → link the second name to it → open overview → assert the circular logo renders and the display name replaces the raw counterparty name for both
- [x] 4.6 E2e placeholder test: upload CSV with a third GUID-suffixed name, never map or link it, assert the overview shows the placeholder for it
- [x] 4.7 Unit tests: the backfill is idempotent — it fills `NameOtherPartyNormalized` for existing transactions and leaves already-filled rows untouched
- [x] 4.8 Unit tests: the `GET /merchants/options` query returns both `Mapped` and `None` merchants, ordered by folded transaction count descending, filters by `q`, and caps at ~10
- [x] 4.9 E2e: link-picker empty state — search a GUID-suffixed name with no matching merchant, choose "create as a new merchant", assert the row becomes a mapped merchant and the overview renders it
- [x] 4.10 Run the full unit and e2e suites and confirm they pass
