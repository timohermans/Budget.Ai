## Context

The Django app (`budget-django/`) implements a small budget tool: a monthly overview with budget/spent/left totals, per-week cards, per-IBAN balances, Rabobank CSV import, and an htmx toggle-fixed interaction. A green Playwright e2e suite (`budget-playwright/`) already encodes the externally visible contract, including test-mode auth via the `X-Test-User` header. This change ports the app to .NET 10 MVC bug-for-bug, validated by pointing the existing suite at the new app. See proposal.md for the motivation.

Constraints that shape the design:

- The e2e suite runs against whatever is on the base URL, authenticating via `X-Test-User` (so Keycloak is not needed to validate the port).
- Exactly one piece of domain logic is shared across endpoints today (`get_summary_for`), which fits the "share only domain logic" rule.
- Upload error templates are empty by design; the suite does not assert on upload errors (bug-for-bug).

## Goals / Non-Goals

**Goals:**

- A behavior-preserving .NET 10 MVC app that passes the full Playwright suite unchanged.
- One thin feature controller per endpoint; the only shared code lives in `Budget.Web/Domain/Transactions/`.
- Unit tests (MSTest v4) for the ported domain logic, named `<Method>_When<Scenario>_Then<Result>`.

**Non-Goals:**

- No UI improvements or error-message content (bug-for-bug; empty error templates stay).
- No user management table (YAGNI, deferred).
- No caching, admin section, or bulk-insert libraries.
- No removal/reshuffle of `budget-django/` (deferred; it stays as reference).
- No separate Domain project - the domain lives inside `Budget.Web`.

## Decisions

### D1. MVC with feature controllers (not Minimal API / Razor Pages)

Each endpoint is a thin controller in `Features/<Area>/<Name>Controller.cs` with a single action that calls domain logic in `Domain/Transactions/` and renders a Razor view.

- Why: this is an HTML/htmx app; Razor views, partials, layouts, and antiforgery are first-class in MVC. Minimal API has no view engine - it would require bolting on `AddControllersWithViews()` plus a custom Razor-to-string renderer just to serve templates. Razor Pages maps awkwardly to the multi-route overview page and to returning bare partials.
- REPR-in-spirit: one controller per resource, zero cross-endpoint code sharing outside the domain.
- Alternatives considered: Minimal API + REPR (rejected: view-rendering plumbing); Razor Pages (rejected: partial returns and route mapping friction).

### D2. Project layout

```
budget-dotnet/
├── Budget.slnx
├── Budget.Web/
│   ├── Domain/Transactions/            # the ONLY shared code
│   │   ├── Transaction.cs
│   │   ├── TransactionClassifier.cs
│   │   ├── Summary.cs                  # records: Summary / WeekSummary / BalanceSummary
│   │   ├── SummaryCalculator.cs        # port of get_summary_for
│   │   └── RabobankCsvImporter.cs      # pre-query dedup
│   ├── Features/Budget/OverviewController.cs
│   ├── Features/Transactions/UploadController.cs
│   ├── Features/Transactions/ToggleFixedController.cs
│   ├── Views/                          # per-controller convention + Shared
│   ├── Data/BudgetDbContext.cs
│   └── Program.cs
└── Budget.Tests/                       # MSTest v4, references Budget.Web
    └── Domain/                         # SummaryCalculatorTests, ClassifierTests, CsvImporterTests
```

`Budget.Tests` references `Budget.Web` and unit-tests the `Domain` classes without starting the web host.

### D3. EF Core + Npgsql, not Dapper

Persistence uses `Microsoft.EntityFrameworkCore` with `Npgsql.EntityFrameworkCore.PostgreSQL` and `UseSnakeCaseNamingConvention()`.

- Why: the data-access surface is tiny (one windowed read, one update, one conditional insert); the real complexity is the in-memory summary calculation. EF gives migrations, `DateOnly`/`decimal` mapping, and the snake_case convention for free. The dedup concern is solved in app code (D5), so EF's lack of native `ON CONFLICT DO NOTHING` is a non-issue.
- Alternatives considered: Dapper (rejected: hand-written SQL and migrations, no snake_case convention); `EFCore.BulkExtensions` (rejected: unnecessary for monthly statement volume).

### D4. Data model - no user table

`Transaction` maps to a `transactions` table with a `user_id` string column that holds the OIDC `sub` claim (or the `X-Test-User` value in test mode). A unique index on `(iban, follow_number, user_id)` enforces dedup. Columns are snake_case via the naming convention.

- Why: YAGNI - the app never queries users, and a Keycloak `sub` is a stable identifier. Avoiding a users table also simplifies test-mode auth (no find-or-create, no DB hit).
- Migration path if a user table is ever needed: a follow-up change; the string id maps cleanly to an external subject column.

### D5. CSV dedup via pre-query and filter

`RabobankCsvImporter` parses the uploaded file, queries the existing `(iban, follow_number)` keys for the user, filters those out, and inserts the remainder.

- Why: simple, explicit, and correct for monthly statement volume. The unique index remains as a backstop.

### D6. Domain port specifics

- `SummaryCalculator` ports `get_summary_for` (see specs - budget-app, budget calculation requirement):
  - Month windows via `new DateTime(year, month, 1)` + `AddMonths`/`AddDays`.
  - Week numbers via `System.Globalization.ISOWeek.GetWeekOfYear`, NOT the culture calendar.
  - Own accounts = the user's IBANs ordered by transaction count descending; default main account = most frequent IBAN.
  - Empty-account edge case returns a zero summary (all totals 0, no weeks) so the dashboard renders empty.
  - Week budget = `budget / days-in-month * days-in-week`; `left = abs(budget) - abs(spent)`; income/expenses from previous-month fixed transactions on the main account; spent from this-month variable expenses on the main account; per-IBAN balances net this month.
  - Sign conventions preserved exactly (expenses stored negative, spent accumulated positive).
- `TransactionClassifier` ports `is_fixed` with the exact rule order and string checks (case-insensitive "paypal"/"sparen").
- `RabobankCsvImporter` ports `process_file`: latin-1 encoding, comma-delimited quoted CSV with header row, amounts with comma decimals, 18-char zero-padded follow numbers, description = concatenation of `Omschrijving-1..3` trimmed.

### D7. Authentication and test mode

- Production: cookie authentication + `Microsoft.AspNetCore.Authentication.OpenIdConnect` pointing at the same Keycloak realm. The `sub` claim becomes the transaction `user_id`. A fallback authorization policy requires an authenticated user on all routes; unauthenticated requests challenge via OIDC.
- Test mode: a middleware that, when `X-Test-User` is present, builds a `ClaimsPrincipal` whose `sub` is the header value, sets `HttpContext.Items["TestMode"] = true`, and short-circuits to the authenticated identity. No user record, no DB access.
- Antiforgery: the two POST actions use a `TestModeAwareValidateAntiforgeryToken` filter that skips validation when the `TestMode` flag is present (mirrors Django's `_dont_enforce_csrf_checks`), so the e2e API-request uploads pass. Production forms render a hidden antiforgery token that htmx submits with the form.

### D8. Formatting contract

All server-rendered numbers/dates use explicit invariant formatting (never ambient server culture):

- Progress bar `value`/`max` attributes with a decimal point (the e2e parses them with InvariantCulture; a comma would parse as zero).
- Month display via `CultureInfo.InvariantCulture` `"MMMM"` (English month names).
- Transaction dates as `"dd-MM"`.

### D9. Views and routing

- Controller class names `OverviewController`, `UploadController`, `ToggleFixedController`; view discovery resolves them to `Views/Overview/`, `Views/Upload/`, `Views/ToggleFixed/` (pure convention, no explicit paths). `Views/Shared/_Layout.cshtml` ports `base.html` (CDN links for Pico.css, Bootstrap grid, htmx, Alpine.js, Lucide, and the htmx `hx-headers` antiforgery header). `Views/Shared/_ToggleFixed.cshtml` holds the toggle partial because it is rendered both by the overview page and by the toggle endpoint.
- Routes: `GET /budget/`, `/budget/{year:int}/{month:int}`, `/budget/{year:int}/{month:int}/{week:int}`, `/budget/{year:int}/{month:int}/{iban}`; `POST /transactions/upload`; `POST /transactions/toggle-fixed`. The `{week:int}` route is registered before the `{iban}` route to disambiguate numeric segments.
- The toggle endpoint recomputes the summary for the transaction's month using the default (main) account and returns the partial with htmx OOB swaps targeting `#spent-total`, `#left-total`, `#spent-week-N`, `#left-week-N`, `#progress-week-N`.
- Upload returns an HTTP 302 redirect to `/budget/{year}/{month}` of the most recent transaction (the e2e asserts 302 with `MaxRedirects=0`).

### D10. Unit tests

MSTest v4 in `Budget.Tests` referencing `Budget.Web`, naming `<Method>_When<Scenario>_Then<Result>`. Coverage mirrors the Django tests and e2e pins:

- `SummaryCalculatorTests`: baseline budget, spent/left, week distribution, fixed income/expenses from previous month, own-account transfer exclusion, PayPal exclusion, empty-account zero summary, IBAN balances.
- `TransactionClassifierTests`: all `is_fixed` rules.
- `RabobankCsvImporterTests`: header-only, single row, duplicate handling, amount/date/description parsing.

## Risks / Trade-offs

- **Ambiguous route (`week:int` vs `iban`)** -> register the constrained route first and validate via the e2e week-navigation tests; if ordering proves flaky, fall back to a single combined segment disambiguated in the handler.
- **Locale-dependent rendering breaks e2e** -> all formatting explicitly invariant (D8); never rely on server culture.
- **Wrong week numbers** -> use `ISOWeek.GetWeekOfYear`; the week-budget e2e tests pin exact values and will catch regressions.
- **htmx + antiforgery in production** -> forms carry hidden tokens; the toggle partial is returned as a fragment (no validation on the response path); covered by the e2e toggle tests which run in test mode.
- **Bug-for-bug quirk preserved by mistake vs. intentionally** -> the toggle-recompute-using-main-account behavior is intentionally preserved; noted here so it is not "fixed" during the port.

## Migration Plan

1. Scaffold `budget-dotnet/` with `Budget.slnx`, `Budget.Web`, `Budget.Tests`.
2. Port domain logic (classifier, calculator, CSV importer) with unit tests - green before any web wiring.
3. Add EF Core `BudgetDbContext`, `Transaction` mapping, and an initial migration.
4. Port views (`_Layout`, overview, toggle partial) and the three feature controllers with routing.
5. Wire auth (OIDC + cookie), test-mode middleware, and the antiforgery filter.
6. Run the Playwright suite against the .NET app (`PLAYWRIGHT_BASE_URL=http://localhost:<port>`) and iterate to green.
7. Rollback: Django app remains available on its own port; switching back is a base-URL change.

## Open Questions

- Redirect semantics for upload after the migration (current: 302, pinned by the e2e suite) - deferred per user preference; revisit only if the e2e contract changes.
