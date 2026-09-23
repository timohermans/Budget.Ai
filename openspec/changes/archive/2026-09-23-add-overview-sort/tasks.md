## 1. Sort plumbing in the Overview feature

- [x] 1.1 Add an `OverviewSort` enum (default `DateDesc`, plus `DateAsc`, `AmountDescHoogsteUitgaves`, `AmountAscKleinsteUitgaves`, `NameAsc`, `NameDesc`) with kebab-string parsing to `Features/Budget` (invalid/missing → default); verify with a unit test in `tests/Budget.Tests` covering each value and a garbage value
- [x] 1.2 Add `Sort` binding + `OverviewViewModel.Sort` to `OverviewController.Index` (`string? sort` query parameter) and apply the sort in-place to `summary.Weeks.Values` and `summary.IbanBalances.Values` transaction lists per the design key table (all options tie-break on date descending, names ignore case); verify via unit tests: hoogste uitgaves orders `-450, -50, -3.50, +25, +1000`, kleinste uitgaves orders `-3.50, -50, -450, +25, +1000`, name sort uses `DisplayName ?? NameOtherParty` case-insensitively
- [x] 1.3 Confirm `build Budget.slnx` and existing `dotnet test tests/Budget.Tests/Budget.Tests.csproj` still pass

## 2. View: dropdown and URL threading

- [x] 2.1 Add the sort `<select data-testid="sort-select">` below the stats card in `Views/Overview/Index.cshtml` with the six Dutch labels in spec order, preselecting the current sort, `hx-get` to the current page including the expanded week/IBAN segment, `hx-push-url`, and verify it fetches without full reload (htmx swap) while keeping the expansion
- [x] 2.2 Thread the sort through month navigation (prev/today/next), week summary, and IBAN summary URLs (`?sort=` omitted for default); verify by hand with `dotnet run --project src/Budget.Web` + `ASPNETCORE_ENVIRONMENT=Development`: select a sort, click prev month and expand another week, confirm the order persists and the URL carries `sort`
- [x] 2.3 Verify the fixed income/expense detail card is unaffected by sort and its URLs do not carry `sort`

## 3. E2E coverage

- [x] 3.1 Add E2E scenarios in `tests/Budget.E2e` using `data-testid="sort-select"`: each of the six orders asserts transaction sequence via `data-testid` rows (date/amount/name), ties break by date descending, and the expanded section stays expanded after changing sort
- [x] 3.2 Add E2E scenarios for URL state: deep-link `?sort=` renders that order, invalid `sort` falls back to default, and sort survives month navigation and week/IBAN expansion
- [x] 3.3 Run the app and full E2E suite (`dotnet test tests/Budget.E2e/Budget.E2e.csproj`) and confirm all pass