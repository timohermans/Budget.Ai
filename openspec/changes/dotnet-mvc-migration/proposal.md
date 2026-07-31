## Why

The budget application currently lives in Django (`budget-django/`) and the project is consolidating on the .NET stack. The Playwright e2e suite (`budget-playwright/`) is green and now acts as a behavioral safety net, making a safe, behavior-preserving migration to .NET 10 MVC possible.

## What Changes

- **BREAKING**: Replace the Django web app with a .NET 10 MVC app in a new `budget-dotnet/` folder, containing `Budget.slnx` with two projects: `Budget.Web` and `Budget.Tests`.
- Port the domain logic to C#: budget summary calculation (`get_summary_for`), transaction fixed/variable classification, and the Rabobank CSV import (latin-1, comma-delimited, dedup on `(iban, follow_number, user)`).
- Port the HTML templates to Razor bug-for-bug, keeping the CDN frontend stack (htmx, Alpine.js, Pico.css, Lucide, Bootstrap grid).
- Persist transactions in PostgreSQL via EF Core with `UseSnakeCaseNamingConvention`. No user table: transactions store the OIDC `sub` claim value directly as `user_id` (YAGNI).
- Keep authentication with the same OIDC provider (Keycloak) using the ASP.NET Core OpenID Connect handler + cookie session.
- Replicate test-mode authentication (`X-Test-User` header) without creating user records - the header value IS the user id - and bypass antiforgery validation in test mode.
- Add unit tests (MSTest v4) for the ported domain logic with naming convention `<Method>_When<Scenario>_Then<Result>`.
- The Django app stays in place as the reference implementation until the .NET app passes the full e2e suite; removal is a separate follow-up.

## Capabilities

### New Capabilities
- `budget-app`: The .NET MVC budget application - budget overview rendering and navigation, budget calculation engine, CSV upload/import, toggle-fixed with htmx OOB swaps, transaction persistence and per-user isolation, and formatting contracts the e2e suite depends on.

### Modified Capabilities
- `test-mode-auth`: Requirements change because the .NET app has no user table - the `X-Test-User` value is used directly as the user id (no "find or create a User"), and test mode SHALL bypass antiforgery validation so the Playwright API requests (which send no CSRF token) succeed.

## Impact

- **New code**: `budget-dotnet/` with `Budget.Web` (MVC feature controllers, `Domain/` with the shared logic, Razor views, EF Core `BudgetDbContext`) and `Budget.Tests`.
- **Database**: New PostgreSQL schema (EF migrations) - `transactions` table with snake_case columns and a unique index on `(iban, follow_number, user_id)`.
- **Existing systems**: `budget-playwright/` runs unchanged against the .NET app by pointing `PLAYWRIGHT_BASE_URL` at it; `budget-django/` remains as reference during migration.
- **Dependencies**: .NET 10 SDK, `Microsoft.EntityFrameworkCore` + `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.AspNetCore.Authentication.OpenIdConnect`, MSTest v4.
