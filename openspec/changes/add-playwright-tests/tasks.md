## 1. Django Test-Mode Auth Middleware

- [ ] 1.1 Add `TestModeAuthMiddleware` to `core/middleware.py` that reads `X-Test-User` header, creates/finds the User, and logs them in
- [ ] 1.2 Insert `TestModeAuthMiddleware` before `CustomLoginRequiredMiddleware` in Django settings `MIDDLEWARE` list above `CustomLoginRequiredMiddleware`

## 2. .NET Playwright Project Scaffold

- [ ] 2.1 Create `budget-playwright/` directory with solution file `Budget.Playwright.sln`
- [ ] 2.2 Create MSTest v4 project `Budget.Playwright.csproj` with `Microsoft.Playwright` and `MSTest` packages
- [ ] 2.3 Add `MSTestSettings.cs` with assembly-level `[Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]`
- [ ] 2.4 Add `Directory.Build.props` for Playwright browser download configuration
- [ ] 2.5 Create directory structure: Pages/, Support/, Tests/

## 3. Test Infrastructure

- [ ] 3.1 Create `PlaywrightFixture.cs` — singleton that installs Playwright, launches Chromium, provides `NewPageAsync()`, with thread-safe lazy initialization
- [ ] 3.2 Create `PlaywrightTestBase.cs` — abstract base with `[TestInitialize]` (create page + user GUID + set X-Test-User header) and `[TestCleanup]` (close page) and `UploadCsv()` helper
- [ ] 3.3 Create `Routes.cs` — static class with all URL methods configurable via `PLAYWRIGHT_BASE_URL` env var

## 4. CSV Test Data Utility

- [ ] 4.1 Create `CsvBuilder.cs` with methods: `AddTransaction()`, `Build()` → `string`
- [ ] 4.2 Implement Rabobank CSV format: semicolons, latin-1 encoding, comma decimal separators, correct column headers
- [ ] 4.3 Create `TestTransaction.cs` record with all fields (date, amount, code, iban, iban_other_party, name_other_party, description, follow_number)
- [ ] 4.4 Create `TestDataFactory.cs` with helper methods for common scenarios (single transaction, month of expenses, budget baseline, own-account transfer, PayPal etc.)

## 5. Page Objects

- [ ] 5.1 Create `BudgetPage.cs` — methods to navigate, get week cards, get IBAN balances, read spent/left totals, get progress bar, expand/collapse weeks
- [ ] 5.2 Create `UploadPage.cs` — method to upload CSV from string (using `FilePayload`), wait for redirect, get error message if present
- [ ] 5.3 Create `WeekCardComponent.cs` — methods to read budget/spent/left values, progress bar percentage, get transaction rows, click toggle on a transaction

## 6. Budget Dashboard Tests (DashboardTests.cs)

- [ ] 6.1 Test: `EmptyDashboard_ShowsCurrentMonth`
- [ ] 6.2 Test: `Dashboard_DefaultRoute_RedirectsToCurrentMonth`
- [ ] 6.3 Test: `Dashboard_SpecificMonth_ShowsCorrectData`
- [ ] 6.4 Test: `Dashboard_MonthNavigation_ChangesData`
- [ ] 6.5 Test: `Dashboard_WeekCard_ShowsBudgetSpentLeft`
- [ ] 6.6 Test: `Dashboard_WeekCard_ProgressBarReflectsPercentage`
- [ ] 6.7 Test: `Dashboard_WeekCard_ExpandShowsTransactions`
- [ ] 6.8 Test: `Dashboard_WeekExpansion_UrlParamExpandsCorrectWeek`
- [ ] 6.9 Test: `Dashboard_IbanBalances_ShowCorrectValues`
- [ ] 6.10 Test: `Dashboard_TransactionsList_FixedVsVariableStyling`

## 7. CSV Upload Tests (UploadTests.cs)

- [ ] 7.1 Test: `Upload_ValidCsv_CreatesAndRedirectsToBudget`
- [ ] 7.2 Test: `Upload_ValidCsv_RedirectsToCorrectMonth`
- [ ] 7.3 Test: `Upload_DuplicateCsv_DoesNotCreateDuplicates`
- [ ] 7.4 Test: `Upload_MalformedCsv_ShowsInlineError`
- [ ] 7.5 Test: `Upload_MissingFile_ShowsInlineError`

## 8. Toggle Fixed Tests (ToggleFixedTests.cs)

- [ ] 8.1 Test: `Toggle_VariableToFixed_UpdatesTransactionDisplay`
- [ ] 8.2 Test: `Toggle_FixedToVariable_UpdatesTransactionDisplay`
- [ ] 8.3 Test: `Toggle_UpdatesSpentTotal_OobSwap`
- [ ] 8.4 Test: `Toggle_UpdatesLeftTotal_OobSwap`
- [ ] 8.5 Test: `Toggle_UpdatesProgressBar_OobSwap`
- [ ] 8.6 Test: `Toggle_MultipleClicks_WorksRepeatedly`

## 9. Budget Calculation Tests (BudgetCalculationTests.cs)

- [ ] 9.1 Test: `Budget_Baseline_IncomeMinusExpensesEqualsBudget`
- [ ] 9.2 Test: `Budget_Spent_OnlyIncludesVariableExpenses`
- [ ] 9.3 Test: `Budget_Left_EqualsBudgetMinusSpent`
- [ ] 9.4 Test: `Budget_WeekDistribution_ProportionalToDays`
- [ ] 9.5 Test: `Budget_FixedIncomeFromLastMonth_Counted`
- [ ] 9.6 Test: `Budget_FixedExpensesFromLastMonth_Counted`
- [ ] 9.7 Test: `Budget_OwnAccountTransfers_Excluded`
- [ ] 9.8 Test: `Budget_PayPalTransactions_Excluded`

## 10. Final Validation

- [ ] 10.1 Run `dotnet build` — verify compilation with no errors
- [ ] 10.2 Run `dotnet test` — verify all ~29 tests pass against running Django instance in test mode
- [ ] 10.3 Verify parallel execution produces no test collisions across 3+ concurrent workers
