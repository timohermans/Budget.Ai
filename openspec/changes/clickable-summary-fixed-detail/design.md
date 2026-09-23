## Context

The budget overview page (`OverviewController.Index`) renders a summary with income and expense totals from fixed transactions of the previous month. These values are currently plain `<span>` elements. The existing htmx patterns use query parameters for state (week expansion via URL path segment, IBAN selection via URL path segment). The `Summary` model already tracks `IncomeTransactions` and `ExpenseTransactions` for budget calculation, but only for the previous month.

## Goals / Non-Goals

**Goals:**
- Make the income and expense summary values clickable to reveal a detail card of fixed transactions from the previous month that contribute to those totals
- The card state is URL-representable via `?fixed=income` or `?fixed=expenses`
- The card can be dismissed (close button or navigating away)
- Follow existing htmx patterns and code conventions

**Non-Goals:**
- Changing the budget calculation logic or what counts as fixed income/expense
- Making week or IBAN sections behave differently
- Adding filtering or sorting to the detail card beyond what the existing transaction list shows

## Decisions

**Query parameter `?fixed=<type>` for URL state**
The existing routes (`/budget/`, `/budget/{year}/{month}`, `/budget/{year}/{month}/{weekOrIban}`) use path segments for week and IBAN selection. A query parameter is cleaner for the fixed detail because it doesn't conflict with the `weekOrIban` route constraint (which expects an int or string that isn't parseable as int). The controller checks `Request.Query["fixed"]` to decide whether to return a partial or full view.

**Partial view `_FixedTransactions.cshtml` returned when `fixed` param is present**
When the `fixed` query parameter is present, the controller returns `PartialView("_FixedTransactions", model)` instead of the full view. The partial renders just the card HTML using the existing `Summary.IncomeTransactions` or `Summary.ExpenseTransactions` lists (which already contain last month's fixed transactions). The main view includes a `<div id="fixed-detail">` container that htmx targets with `hx-swap="innerHTML"`. This follows the existing pattern of returning partials for htmx-driven updates (see `_ToggleFixed.cshtml`).

**Close button uses `hx-get` to navigate to URL without `fixed` param**
The card includes a close button that links to the same month URL without the query parameter. It uses `hx-get`, `hx-target="#fixed-detail"`, `hx-swap="innerHTML"`, and `hx-push-url="true"` to remove the card and update the URL. This mirrors the existing pattern of using htmx on links for SPA-like navigation.

**Loading indicator on the clickable values**
The income and expense values use `hx-indicator` to show a spinning loader while the partial is being fetched, following the existing pattern on the upload form.

**E2E tests verify URL state changes**
Tests verify that clicking the income/expense values updates the URL to include `?fixed=income` or `?fixed=expenses`, and that the close button or month navigation removes the parameter from the URL.

## Risks / Trade-offs

- **htmx `innerHTML` swap clears the container on re-navigation**: If the user clicks income, then navigates to a different month, the `#fixed-detail` container is replaced with the new page content. The card is naturally dismissed. This is acceptable.
- **No toggle-off by clicking the same value again**: Clicking the income value again when the card is already open will re-fetch and re-render the same card. A toggle-off behavior could be added later but isn't in scope.
- **`hx-push-url="true"` on the income/expense clicks**: This pushes the URL with `?fixed=income` to browser history. The browser back button will navigate to the URL without the parameter, which removes the card. This is the desired behavior.