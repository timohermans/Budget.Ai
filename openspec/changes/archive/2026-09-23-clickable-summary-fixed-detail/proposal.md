## Why

The budget overview shows fixed income and expenses as plain text in the summary card. Users cannot see the detail of which transactions contribute to those totals without expanding weeks or IBAN sections and manually scanning for fixed transactions. Making these values clickable provides instant access to the underlying fixed transactions for the displayed month.

## What Changes

- Make the `data-testid="income-value"` and `data-testid="expenses-value"` elements clickable
- Clicking a value fetches and displays a detail card showing all fixed income or fixed expense transactions from the previous month that contribute to those summary totals
- The detail card is URL-representable via a `?fixed=income` or `?fixed=expenses` query parameter so users can navigate away and return, and share links
- The detail card can be dismissed by clicking a close button or navigating to a different month

## Capabilities

### New Capabilities
- `fixed-transaction-detail`: Clickable summary values that fetch and display a card of fixed income or expense transactions from the previous month (the same transactions that contribute to the summary totals), with URL state via query parameter

### Modified Capabilities
- `budget-app`: The overview page now supports a `?fixed=<type>` query parameter that renders a fixed-transaction detail card inline; the income and expense summary values are interactive

## Impact

- `OverviewController.cs` — new query parameter handling, partial view return path
- `Views/Overview/Index.cshtml` — clickable income/expense values, `#fixed-detail` container
- `Views/Shared/_FixedTransactions.cshtml` — new partial view for the detail card
- E2E tests — new test scenarios for the clickable summary values and detail card visibility