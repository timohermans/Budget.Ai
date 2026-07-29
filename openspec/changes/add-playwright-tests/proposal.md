## Why

The budget-django application has only minimal backend unit tests and zero end-to-end test coverage. A full E2E suite is needed to verify the app's actual behavior before rewriting it in .NET 10 Razor Pages — the Playwright tests will serve as the regression baseline that the .NET version must also pass.

## What Changes

- Add a test-mode auth middleware to the Django app that accepts `X-Test-User` header and bypasses OIDC
- Create a new .NET MSTest v4 Playwright solution alongside the Django project
- Build a CsvBuilder utility to generate Rabobank-format CSV test data
- Implement page object models for the budget dashboard and upload flows
- Write ~30 E2E tests covering: dashboard rendering, month navigation, CSV upload, toggle-fixed, and budget calculation correctness
- All tests are isolated via per-test user GUIDs, enabling safe parallel execution

## Capabilities

### New Capabilities
- `test-mode-auth`: Django middleware that authenticates a test user when `X-Test-User` header is present, bypassing OIDC
- `playwright-test-suite`: .NET Playwright test project with MSTest v4, covering all UI flows and budget calculations

### Modified Capabilities
<!-- No existing capabilities change — this is all additive -->

## Impact

- **budget-django/**: One new middleware file (`core/middleware.py` already exists, add to it or create `core/auth.py`) and a settings change to insert it in the middleware chain
- **New directory**: `budget-playwright/` containing the .NET solution
- **New dependencies**: `Microsoft.Playwright`, `MSTest` (v4 meta-package)
- **No production code changes** — test mode is inactive unless `X-Test-User` header is present
