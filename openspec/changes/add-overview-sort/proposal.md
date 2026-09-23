## Why

The budget overview lists transactions in a single fixed order (date descending). Users cannot re-order the lists to see their biggest expenses first, hunt small charges, or find a shop alphabetically — they must scan the whole month manually. A sort control with the order kept in the URL makes this a shareable, navigation-safe part of the page.

## What Changes

- Add a sort dropdown below the summary card on the budget overview
- Six sort options with Dutch labels: `datum (eind - begin)` (default), `datum (begin - eind)`, `hoogste uitgaves`, `kleinste uitgaves`, `winkel (a - z)`, `winkel (z - a)`
- The selected sort is URL state via a `?sort=` query parameter, applied server-side
- Sort applies to the transaction lists of the expanded week and the expanded IBAN section; the fixed-transaction detail card is unaffected
- Changing the dropdown fetches the page via htmx (no full reload) while keeping the currently expanded week or IBAN, and pushes the new URL
- The sort survives navigation: month navigation, week expansion, and IBAN expansion links carry the current sort

## Capabilities

### New Capabilities

### Modified Capabilities

- `budget-app`: The overview page gains a transaction sort control; sort order is URL-representable via `?sort=`, applied to expanded week and IBAN transaction lists, and preserved across navigation

## Impact

- `src/Budget.Web/Features/Budget/OverviewController.cs` — new `sort` query parameter, parse to a sort enum, apply to computed summary lists
- `src/Budget.Web/Views/Overview/Index.cshtml` — sort dropdown below the stats card, `?sort=` threaded through month navigation, week summary, and IBAN summary URLs
- `tests/Budget.E2e` — E2E scenarios for the sort dropdown (order assertions, URL state, navigation persistence)