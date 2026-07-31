## 1. Django Test-Mode Auth Middleware

- [x] 1.1 Add `TestModeAuthMiddleware` to `core/middleware.py` that reads `X-Test-User` header, creates/finds the User, and logs them in
- [x] 1.2 Insert `TestModeAuthMiddleware` before `CustomLoginRequiredMiddleware` in Django settings `MIDDLEWARE` list above `CustomLoginRequiredMiddleware`

## 2. .NET Playwright Project Scaffold

- [x] 2.1 Create `budget-playwright/` directory with solution file `Budget.Playwright.sln`
- [x] 2.2 Create MSTest v4 project `Budget.Playwright.csproj` with `Microsoft.Playwright` and `MSTest` packages
- [x] 2.3 Add `MSTestSettings.cs` with assembly-level `[Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]`
- [x] 2.4 Add `Directory.Build.props` for Playwright browser download configuration
- [x] 2.5 Create directory structure: Pages/, Support/, Tests/

## 3. Test Infrastructure

- [x] 3.1 Create `PlaywrightFixture.cs` — singleton that installs Playwright, launches Chromium, provides `NewPageAsync()`, with thread-safe lazy initialization
- [x] 3.2 Create `PlaywrightTestBase.cs` — abstract base with `[TestInitialize]` (create page + user GUID + set X-Test-User header) and `[TestCleanup]` (close page) and `UploadCsv()` helper
- [x] 3.3 Create `Routes.cs` — static class with all URL methods configurable via `PLAYWRIGHT_BASE_URL` env var

## 4. CSV Test Data Utility

- [x] 4.1 Create `CsvBuilder.cs` with methods: `AddTransaction()`, `Build()` → `string`
- [x] 4.2 Implement Rabobank CSV format: semicolons, latin-1 encoding, comma decimal separators, correct column headers
- [x] 4.3 Create `TestTransaction.cs` record with all fields (date, amount, code, iban, iban_other_party, name_other_party, description, follow_number)
- [x] 4.4 Create `TestDataFactory.cs` with helper methods for common scenarios (single transaction, month of expenses, budget baseline, own-account transfer, PayPal etc.)

## 5. Page Objects

- [x] 5.1 Create `BudgetPage.cs` — methods to navigate, get week cards, get IBAN balances, read spent/left totals, get progress bar, expand/collapse weeks
- [x] 5.2 Create `UploadPage.cs` — method to upload CSV from string (using `FilePayload`), wait for redirect, get error message if present
- [x] 5.3 Create `WeekCardComponent.cs` — methods to read budget/spent/left values, progress bar percentage, get transaction rows, click toggle on a transaction

## 6. Budget Dashboard Tests (DashboardTests.cs)

- [x] 6.1 Test: `EmptyDashboard_ShowsCurrentMonth`
- [x] 6.2 Test: `Dashboard_DefaultRoute_RedirectsToCurrentMonth`
- [x] 6.3 Test: `Dashboard_SpecificMonth_ShowsCorrectData`
- [x] 6.4 Test: `Dashboard_MonthNavigation_ChangesData`
- [x] 6.5 Test: `Dashboard_WeekCard_ShowsBudgetSpentLeft`
- [x] 6.6 Test: `Dashboard_WeekCard_ProgressBarReflectsPercentage`
- [x] 6.7 Test: `Dashboard_WeekCard_ExpandShowsTransactions`
- [x] 6.8 Test: `Dashboard_WeekExpansion_UrlParamExpandsCorrectWeek`
- [x] 6.9 Test: `Dashboard_IbanBalances_ShowCorrectValues`
- [x] 6.10 Test: `Dashboard_TransactionsList_FixedVsVariableStyling`

## 7. CSV Upload Tests (UploadTests.cs)

- [x] 7.1 Test: `Upload_ValidCsv_CreatesAndRedirectsToBudget`
- [x] 7.2 Test: `Upload_ValidCsv_RedirectsToCorrectMonth`
- [x] 7.3 Test: `Upload_DuplicateCsv_DoesNotCreateDuplicates`
- [x] 7.4 Test: `Upload_MalformedCsv_ShowsInlineError`
- [x] 7.5 Test: `Upload_MissingFile_ShowsInlineError`

## 8. Toggle Fixed Tests (ToggleFixedTests.cs)

- [x] 8.1 Test: `Toggle_FixedToVariable_UpdatesWeekCardAndBudgetTotals`
- [x] 8.2 Test: `Toggle_VariableToFixed_UpdatesWeekCardAndBudgetTotals`

## 9. Budget Calculation Tests (BudgetCalculationTests.cs)

- [x] 9.1 Test: `Budget_Baseline_IncomeMinusExpensesEqualsBudget`
- [x] 9.2 Test: `Budget_Spent_OnlyIncludesVariableExpenses`
- [x] 9.3 Test: `Budget_Left_EqualsBudgetMinusSpent`
- [x] 9.4 Test: `Budget_WeekDistribution_ProportionalToDays`
- [x] 9.5 Test: `Budget_FixedIncomeFromLastMonth_Counted`
- [x] 9.6 Test: `Budget_FixedExpensesFromLastMonth_Counted`
- [x] 9.7 Test: `Budget_OwnAccountTransfers_Excluded`
- [x] 9.8 Test: `Budget_PayPalTransactions_Excluded`

## 10. Final Validation

- [x] 10.1 Run `dotnet build` — verify compilation with no errors
- [x] 10.2 Run `dotnet test` — 15/26 tests pass; 11 fail due to budget calculation data visibility (test infrastructure verified: page loads, navigation, upload, toggle all work)
- [ ] 10.3 Verify parallel execution produces no test collisions across 3+ concurrent workers
