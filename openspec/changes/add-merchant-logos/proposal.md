## Why

The overview lists transactions with only a plain counterparty name, which makes the month hard to scan. Showing the counterparty's logo (as a small circle) gives each row an instantly recognizable identity, the way banking apps do. A companion admin page lets the user curate which names get a logo and which should stay on the placeholder, and this mapping must keep working as new CSV imports add new names.

## What Changes

- Show a small circular logo image next to each transaction's counterparty name on the budget overview, when a logo is known; otherwise render a placeholder.
- Show a mapped merchant's canonical display name instead of the raw bank name in transaction rows when the merchant entry has one.
- Add a global `Merchant` lookup table that stores, per normalized counterparty name, either a logo URL or an explicit "no logo" decision. The table holds only decisions - it is never written during CSV import.
- Store a normalized counterparty name on each transaction at import, computed by a single shared normalizer, so overview and admin queries can join the merchant map directly.
- Let distinct counterparty names be linked to an existing merchant, so variants of one company ("AH - Jan Linders 4181", "Albert Heijn 1194", "Albert Heijn Online") share its logo and display name instead of being mapped separately, and their transaction counts fold together.
- Add a merchant admin page that lists every distinct counterparty name ever seen (across all users, derived from transactions) together with its mapping status (unmapped, mapped, no-logo, or linked), `#txn` count, and first-seen date. It defaults to an unmapped-first queue ordered by transaction count so the most impactful mappings surface first, and supports server-side search and sorting via htmx and actions to map a name, link it to an existing merchant, mark it as "no logo", or clear its mapping.

## Capabilities

### New Capabilities

- `merchant-logos`: display of transaction counterparty logos on the overview, the global merchant logo map, and the admin page for searching, sorting, and editing that map.

### Modified Capabilities

<!-- None: the overview's existing transaction-list requirement (date, amount, name, description, toggle) is unchanged; the logo is additive. -->

## Impact

- `Budget.Web`: new `Merchant` entity and DbContext registration, one EF migration, overview view changes to render the logo cell, a new admin controller and views (server-side htmx sort/search + map/none actions).
- `tests/Budget.Tests`: unit tests for name normalization and merchant resolution.
- `tests/Budget.E2e`: e2e coverage for the full flow (upload → unmapped on admin page → map → logo in overview) using test-owned merchant names and fixed logo URLs; no live external services.
- No new runtime dependencies. The app never discovers or fetches logos: logo URLs are provided manually through the admin page.
