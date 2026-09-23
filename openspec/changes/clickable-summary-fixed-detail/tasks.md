## 1. Modify OverviewController to handle `fixed` query parameter

- [ ] 1.1 Add `string? fixed` parameter to `OverviewController.Index`
- [ ] 1.2 When `fixed` is present, return `PartialView("_FixedTransactions", model)` instead of `View(viewModel)`
- [ ] 1.3 Add unit test: controller returns partial view when `fixed` query parameter is present
- [ ] 1.4 Add unit test: controller returns full view when `fixed` query parameter is absent

## 2. Create `_FixedTransactions.cshtml` partial view

- [ ] 2.1 Create `Views/Shared/_FixedTransactions.cshtml` with a card showing fixed transactions from `Summary.IncomeTransactions` or `Summary.ExpenseTransactions`
- [ ] 2.2 Card includes: header with type label (income/expense), close button, and transaction list (date, amount, counterparty, description)
- [ ] 2.3 Close button uses `hx-get` to navigate to the same URL without `?fixed=`

## 3. Make income/expense values clickable in Index.cshtml

- [ ] 3.1 Wrap `data-testid="income-value"` in an element with `hx-get`, `hx-target="#fixed-detail"`, `hx-swap="innerHTML"`, `hx-push-url="true"`, and `hx-indicator` for loading state
- [ ] 3.2 Wrap `data-testid="expenses-value"` in an element with `hx-get`, `hx-target="#fixed-detail"`, `hx-swap="innerHTML"`, `hx-push-url="true"`, and `hx-indicator` for loading state
- [ ] 3.3 Add `<div id="fixed-detail"></div>` container below the summary card
- [ ] 3.4 Add cursor-pointer style to clickable elements

## 4. Add E2E tests for the fixed detail card

- [ ] 4.1 Add test: clicking income value shows fixed income detail card, updates URL to `?fixed=income`, and shows loading indicator
- [ ] 4.2 Add test: clicking expenses value shows fixed expense detail card, updates URL to `?fixed=expenses`, and shows loading indicator
- [ ] 4.3 Add test: clicking close button removes the detail card and clears URL parameter
- [ ] 4.4 Add test: navigating to a different month dismisses the detail card and clears URL parameter
- [ ] 4.5 Add test: browser back button dismisses the detail card and restores the previous URL

## 5. Verify and run all tests

- [ ] 5.1 Run unit tests: `dotnet test tests/Budget.Tests/Budget.Tests.csproj`
- [ ] 5.2 Run E2E tests: `dotnet test tests/Budget.E2e/Budget.E2e.csproj` (app must be running)
- [ ] 5.3 Build the solution: `dotnet build Budget.slnx`