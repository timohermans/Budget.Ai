## 1. Solution scaffolding

- [ ] 1.1 Create `budget-dotnet/` with `Budget.slnx` containing `Budget.Web` (ASP.NET Core MVC, net10.0) and `Budget.Tests` (MSTest v4) projects per design D2
- [ ] 1.2 Create the folder structure in `Budget.Web`: `Domain/Transactions/`, `Features/Budget/`, `Features/Transactions/`, `Views/`, `Data/`
- [ ] 1.3 Add NuGet packages: `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.AspNetCore.Authentication.OpenIdConnect`; add `MSTest.TestFramework`/`MSTest.TestAdapter` to `Budget.Tests`
- [ ] 1.4 Verify `dotnet build` succeeds for the empty solution

## 2. Domain - Transaction classification

- [ ] 2.1 Port `Transaction` entity (`Domain/Transactions/Transaction.cs`): Id, FollowNumber, Iban, Currency, Amount (decimal 10,2), Date (DateOnly), NameOtherParty, IbanOtherParty, Description, IsNotFixed, Code, UserId (string); income/expense helpers
- [ ] 2.2 Port `TransactionClassifier` with the exact `is_fixed` rule order from the Django model (specs - budget-app, transaction classification): flagged variable, own-account income, "paypal", "db"+sparen, "db"+Rabobank, codes sb/cb/bg/ei/tb
- [ ] 2.3 Add `TransactionClassifierTests` covering every rule and the precedence order, named `<Method>_When<Scenario>_Then<Result>`

## 3. Domain - Summary calculator

- [ ] 3.1 Port summary records (`Summary.cs`): `Summary`, `WeekSummary`, `BalanceSummary` mirroring the Django dataclasses
- [ ] 3.2 Port `SummaryCalculator` (`get_summary_for`): month windows, ISOWeek week numbers, own-account detection by frequency, main-account selection, empty-account zero summary, income/expenses from previous month, spent from variable expenses, week budget distribution, per-IBAN balances, sign conventions (design D6)
- [ ] 3.3 Add `SummaryCalculatorTests` covering: baseline budget, spent/left, week distribution, previous-month fixed income/expenses, own-account transfer exclusion, PayPal exclusion, empty account, IBAN balances (mirror the Django manager tests and e2e pins)

## 4. Domain - CSV importer

- [ ] 4.1 Port `RabobankCsvImporter`: latin-1 read, comma-delimited quoted CSV with header row, amount/date parsing, description concatenation, follow-number parsing (design D6)
- [ ] 4.2 Implement dedup by pre-querying existing `(iban, follow_number, user_id)` keys and filtering, per design D5
- [ ] 4.3 Add `RabobankCsvImporterTests`: header-only file, single row field parsing, duplicate handling, missing/invalid file behavior

## 5. Data layer

- [ ] 5.1 Add `BudgetDbContext` (`Data/BudgetDbContext.cs`) mapping `Transaction` with `UseSnakeCaseNamingConvention`, `Amount` as decimal(10,2), and a unique index on `(Iban, FollowNumber, UserId)` (design D3, D4)
- [ ] 5.2 Add the initial EF migration and verify it creates a `transactions` table with snake_case columns and the unique index

## 6. Views

- [ ] 6.1 Port `base.html` to `Views/Shared/_Layout.cshtml`: same CDN links (Pico.css, Bootstrap grid, htmx, Alpine.js, Lucide), the htmx `hx-headers` antiforgery wiring, and the shared styles/scripts (design D8, D9)
- [ ] 6.2 Port the overview page to `Views/Overview/Index.cshtml`: stats, week cards, IBAN balances, transaction lists, week/IBAN expansion, and the month navigation (bug-for-bug, invariant formatting per design D8)
- [ ] 6.3 Port the toggle-fixed partial to `Views/Shared/_ToggleFixed.cshtml` with the htmx OOB swaps targeting `#spent-total`, `#left-total`, `#spent-week-N`, `#left-week-N`, `#progress-week-N` (design D9)
- [ ] 6.4 Add the upload views (`Views/Upload/`): empty error template and success redirect behavior, bug-for-bug

## 7. Feature controllers

- [ ] 7.1 Add `OverviewController` (`Features/Budget/`): single action handling the four GET routes, resolving week vs IBAN segments, computing the summary via `SummaryCalculator`, rendering the overview
- [ ] 7.2 Add `UploadController` (`Features/Transactions/`): reads the uploaded file, calls `RabobankCsvImporter`, returns HTTP 302 redirect to the most recent transaction's month, or the error response (design D9)
- [ ] 7.3 Add `ToggleFixedController` (`Features/Transactions/`): toggles `IsNotFixed`, recomputes the summary for the transaction's month on the main account, renders the partial with OOB swaps (design D9)

## 8. Authentication, test mode, and antiforgery

- [ ] 8.1 Configure cookie + OpenID Connect authentication in `Program.cs` against the existing Keycloak realm, mapping `sub` to `UserId`, with a fallback authorization policy requiring an authenticated user (design D7)
- [ ] 8.2 Add the test-mode middleware: authenticate from `X-Test-User` (value becomes the user id, no user record), set `HttpContext.Items["TestMode"]`, no DB access (design D7; specs - test-mode-auth)
- [ ] 8.3 Add `TestModeAwareValidateAntiforgeryToken` filter that skips validation in test mode; apply to the two POST actions; add hidden antiforgery tokens to the htmx forms (design D7; specs - test-mode-auth)

## 9. Wiring and validation

- [ ] 9.1 Configure routing: register `/budget/{year:int}/{month:int}/{week:int}` before the `{iban}` route; verify all six routes resolve correctly (design D9)
- [ ] 9.2 Run `dotnet build` and all unit tests in `Budget.Tests` until green
- [ ] 9.3 Run the Playwright suite against the .NET app (`PLAYWRIGHT_BASE_URL=http://localhost:<port>`) with a migrated database; iterate until the full suite is green
