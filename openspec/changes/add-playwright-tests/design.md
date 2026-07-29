## Context

The budget-django app (budget/transactions/core apps) uses OIDC auth, HTMX rendering, and a Rabobank CSV import workflow. The existing Django tests cover only backend logic. No tests verify the rendered UI, HTMX interactions, or budget calculations end-to-end.

This design covers two areas: the test-mode auth middleware DLL needed in Django, and the .NET Playwright test project that will be the sole test artifact delivered by this change.

## Goals / Non-Goals

**Goals:**
- Add a test-mode auth middleware to Django that bypasses OIDC via `X-Test-User` header
- Create a .NET MSTest v4 Playwright project with full test coverage
- Achieve complete data isolation between parallel tests via per-test user GUIDs
- Generate Rabobank CSV test data from C# code so no file fixtures are needed

**Non-Goals:**
- Not adding any production API endpoints to Django (the middleware reads a header only)
- Not modifying any existing test files in the Django project
- Not Dockerizing the test environment (tests run against localhost)
- Not covering the admin interface or OIDC auth flow itself

## Decisions

### Decision 1: Test mode as Django middleware, not a view decorator

A middleware at the top of the chain intercepts `X-Test-User` before `LoginRequiredMiddleware`. Using middleware means zero changes to existing views — they just see an authenticated user. A decorator approach would require touching every view.

Alternatives considered:
- **Monkey-patch during test setup**: Requires Playwright to know Django internals. Fragile.
- **Separate Django settings file**: Requires managing multiple settings files. The middleware is simpler.

### Decision 2: Per-test user GUID isolation

Each test generates `Guid.NewGuid()` and sets it as `X-Test-User`. The middleware creates the Django User if it doesn't exist. This is simpler than sharing a user and cleaning up transactions between tests, and it eliminates race conditions under parallel execution at any concurrency level.

### Decision 3: CSV upload for test data seeding (not a seed API)

Tests seed data by uploading Rabobank CSVs through the app's own upload form. This ensures the upload path itself is tested implicitly by every dashboard/calculation test. A seed API would be cleaner but wouldn't test the upload flow. Since the CSV format is stable and the upload is the only way data enters the system, this is the right tradeoff.

### Decision 4: CsvBuilder generates CSV strings (no file fixtures)

Instead of storing `.csv` files on disk, `CsvBuilder` constructs CSV content in memory. This makes test data self-documenting (each test constructs exactly the data it needs) and eliminates file path dependencies. Playwright's `page.SetInputFilesAsync` accepts a `FilePayload` object from a string, so no temp files are needed.

### Decision 5: Routes class with configurable base URL

All URL paths live in a static `Routes` class. The base URL defaults to `http://localhost:8000` and is overridable via environment variable or config. When migrating to .NET Razor Pages, only the URL structure in this class needs updating.

### Decision 6: MSTest v4 with assembly-level parallelization

`[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]` runs all test methods in parallel across all classes. Each test gets its own `IPage` + user GUID. The `PlaywrightFixture` is a singleton that lazily initializes Chromium, thread-safe via double-checked locking.

Alternatives considered:
- **Class-level parallelization**: Reduces parallelism since tests in the same file block each other. Not needed given per-test isolation.
- **No parallelization**: Too slow for ~30 tests.

## Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│                      TEST INFRASTRUCTURE                          │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  PlaywrightFixture (singleton)                                   │
│  ┌─────────────────────────────────────────────────────────┐     │
│  │  InstallAsync() → Playwright.CreateAsync()              │     │
│  │  LaunchAsync() → IBrowser (Chromium, headless)          │     │
│  │  NewPageAsync() → IPage (fresh context per call)        │     │
│  └─────────────────────────────────────────────────────────┘     │
│                                                                  │
│  PlaywrightTestBase (abstract base class)                        │
│  ┌─────────────────────────────────────────────────────────┐     │
│  │  [TestInitialize]: Create page + GUID + set header      │     │
│  │  [TestCleanup]:   Close page                            │     │
│  │  UploadCsv(csv): Upload CSV and follow redirect         │     │
│  └─────────────────────────────────────────────────────────┘     │
│                                                                  │
│  Pages/                                                          │
│  ┌────────────┐  ┌────────────┐  ┌──────────────────────┐       │
│  │ BudgetPage │  │ UploadPage │  │ WeekCardComponent    │       │
│  │ .Navigate()│  │ .Upload()  │  │ .Transactions()      │       │
│  │ .WeekCard()│  │ .Error()   │  │ .Budget(),.Spent()   │       │
│  │ .IbanCard()│  │            │  │ .Left(),.Progress()  │       │
│  └────────────┘  └────────────┘  └──────────────────────┘       │
│                                                                  │
│  Support/                                                        │
│  ┌──────────────────┐  ┌─────────────┐  ┌──────────────┐        │
│  │ CsvBuilder       │  │ TestData    │  │ Routes       │        │
│  │ .AddTransaction()│  │ .Factory()  │  │ .Budget()    │        │
│  │ .Build() → string│  │ (scenarios) │  │ .Upload()    │        │
│  └──────────────────┘  └─────────────┘  │ .ToggleFixed()│       │
│                                          └──────────────┘        │
│                                                                  │
│  Tests/                                                          │
│  ┌──────────────────┐  ┌──────────────  ┌───────────────────┐   │
│  │ DashboardTests   │  │ UploadTests   │ ToggleFixedTests  │   │
│  │ (12 tests)       │  │ (5 tests)     │ (6 tests)         │   │
│  └──────────────────┘  └───────────────┘ └───────────────────┘   │
│  ┌──────────────────┐                                            │
│  │ BudgetCalcTests  │                                            │
│  │ (8 tests)        │                                            │
│  └──────────────────┘                                            │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

## Test Data Flow

```
Test method
│
├─ new CsvBuilder()
│   .AddTransaction(date: Dec 15, amount: 3000, code: "sb", ...)
│   .AddTransaction(date: Dec 15, amount: -800, code: "cb", ...)
│   .AddTransaction(date: Jan 3,  amount: -150, code: "bc", ...)
│   .Build() → "Datum;Bedrag;Munt;...\n2026-12-15;3000,00;EUR;...\n..."
│
├─ UploadCsv(csvString)
│   │
│   ├─ page.SetInputFilesAsync("input[type=file]", new FilePayload {
│   │     Name = "test.csv",
│   │     MimeType = "text/csv",
│   │     Buffer = Encoding.Latin1.GetBytes(csvString)
│   │   })
│   ├─ page.ClickAsync("button[type=submit]")
│   ├─ page.WaitForURLAsync("**/budget/**/")  ← follows redirect
│   └─ returns new BudgetPage(page)
│
├─ budgetPage.WeekCard(1).Budget()    → "€2.200"
├─ budgetPage.WeekCard(1).Spent()     → "€150"
├─ budgetPage.WeekCard(1).Left()      → "€2.050"
├─ budgetPage.WeekCard(1).Progress()  → "6.8%" (150/2200)
│
└─ Assert.AreEqual("€2.200", budget)
```

## Django Middleware Design

```python
# core/middleware.py (add to existing file)

class TestModeAuthMiddleware:
    def __init__(self, get_response):
        self.get_response = get_response

    def __call__(self, request):
        test_user_id = request.META.get("HTTP_X_TEST_USER")
        if test_user_id:
            user, _ = User.objects.get_or_create(
                username=test_user_id,
                defaults={"is_active": True},
            )
            request.user = user
            request.test_mode = True
            from django.contrib.auth import login
            login(request, user, backend="django.contrib.auth.backends.ModelBackend")
        return self.get_response(request)
```

Placed BEFORE `CustomLoginRequiredMiddleware` in `MIDDLEWARE`:
```python
MIDDLEWARE = [
    ...,
    "core.middleware.TestModeAuthMiddleware",  # first custom middleware
    "core.middleware.CustomLoginRequiredMiddleware",
    ...,
]
```

## Routes Class Design

```csharp
public static class Routes
{
    public static string BaseUrl { get; set; } = 
        Environment.GetEnvironmentVariable("PLAYWRIGHT_BASE_URL") ?? "http://localhost:8000";

    public static string Budget() => $"{BaseUrl}/budget/";
    public static string Budget(int year, int month) => $"{BaseUrl}/budget/{year}/{month}/";
    public static string Budget(int year, int month, int week) => $"{BaseUrl}/budget/{year}/{month}/{week}";
    public static string Budget(int year, int month, string iban) => $"{BaseUrl}/budget/{year}/{month}/{iban}";
    public static string Upload => $"{BaseUrl}/transactions/upload";
    public static string ToggleFixed => $"{BaseUrl}/transactions/toggle-fixed";
}
```

## Key Test Scenarios (Full detail in spec files)

| Test Class | # Tests | What |
|---|---|---|
| DashboardTests | 10 | Empty state, month nav, week cards, progress bars, IBAN balances, transaction display, week expansion, URL param filtering |
| UploadTests | 5 | Valid CSV, duplicates, malformed file, missing file, empty file |
| ToggleFixedTests | 6 | Variable→fixed, fixed→variable, OOB updates (spent, left, progress, week), repeated toggles |
| BudgetCalculationTests | 8 | Baseline income/expenses, spent-only-variable, left=budget-spent, own-account exclusion, PayPal exclusion, proportional week distribution, fixed income from last month, fixed expenses from last month |

~29 tests total. Each test fully isolated by user GUID.

## Risks / Trade-offs

- **[Risk] CSV upload is slow** (~1s per upload + redirect). Mitigation: Tests that only need existing data skip the upload step. Budget calc tests batch multiple transactions in a single CSV.
- **[Risk] Django test-mode middleware creates users but never cleans them up**. Mitigation: Test users are GUIDs with no PII. They accumulate in the DB but that's harmless for a local dev environment.
- **[Risk] HTMX OOB swap assertions are fragile** — they depend on specific element IDs in the template. Mitigation: Page object model encapsulates selectors. If templates change, only the page object needs updating.
- **[Risk] MSTest v4 parallel execution with shared Playwright browser instance**. Mitigation: `IBrowser.NewPageAsync()` is thread-safe per Playwright docs. The singleton fixture uses double-checked locking for its async initialization.

## Open Questions

- Should `PLAYWRIGHT_BASE_URL` default to `http://localhost:8000` or to `http://localhost:5000` (a common Playwright default)? Keeping 8000 to match Django's default `runserver` port, but it's configurable either way.
