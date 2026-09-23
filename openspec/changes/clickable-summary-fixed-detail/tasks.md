## 1. Modify OverviewController to handle `fixed` query parameter

- [x] 1.1 Add `string? fixed` parameter to `OverviewController.Index`
- [x] 1.2 When `fixed` is present, return `PartialView("_FixedTransactions", model)` instead of `View(viewModel)`
- [x] 1.3 Add unit test: controller returns partial view when `fixed` query parameter is present
- [x] 1.4 Add unit test: controller returns full view when `fixed` query parameter is absent

## 2. Create `_FixedTransactions.cshtml` partial view

- [x] 2.1 Create `Views/Shared/_FixedTransactions.cshtml` with a card showing fixed transactions from `Summary.IncomeTransactions` or `Summary.ExpenseTransactions`
- [x] 2.2 Card includes: header with type label (income/expense), close button, and transaction list (date, amount, counterparty, description)
- [x] 2.3 Close button uses `hx-get` to navigate to the same URL without `?fixed=`

## 3. Make income/expense values clickable in Index.cshtml

- [x] 3.1 Wrap `data-testid="income-value"` in an element with `hx-get`, `hx-target="#fixed-detail"`, `hx-swap="innerHTML"`, `hx-push-url="true"`, and `hx-indicator` for loading state
- [x] 3.2 Wrap `data-testid="expenses-value"` in an element with `hx-get`, `hx-target="#fixed-detail"`, `hx-swap="innerHTML"`, `hx-push-url="true"`, and `hx-indicator` for loading state
- [x] 3.3 Add `<div id="fixed-detail"></div>` container below the summary card
- [x] 3.4 Add cursor-pointer style to clickable elements

## 4. Add E2E tests for the fixed detail card

- [x] 4.1 Add test: clicking income value shows fixed income detail card, updates URL to `?fixed=income`, and shows loading indicator
- [x] 4.2 Add test: clicking expenses value shows fixed expense detail card, updates URL to `?fixed=expenses`, and shows loading indicator
- [x] 4.3 Add test: clicking close button removes the detail card and clears URL parameter
- [x] 4.4 Add test: navigating to a different month dismisses the detail card and clears URL parameter
- [x] 4.5 Add test: browser back button dismisses the detail card and restores the previous URL

## 5. Verify and run all tests

- [x] 5.1 Run unit tests: `dotnet test tests/Budget.Tests/Budget.Tests.csproj`
- [x] 5.2 Run E2E tests: `dotnet test tests/Budget.E2e/Budget.E2e.csproj` (app must be running)
- [x] 5.3 Build the solution: `dotnet build Budget.slnx`