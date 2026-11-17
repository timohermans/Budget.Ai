# AGENTS.md

Budget app. The deployed application is Django 6.1 (Python 3.12+, tested with 3.14) in `src-django`, using PostgreSQL 18 in production and SQLite for scratch/test runs. Two .NET 10 projects remain: the `Budget.Scraper` console app in `src/Budget.Scraper` and the MSTest + Playwright E2E suite in `tests/Budget.E2e`; `Budget.slnx` (new XML solution format) contains only those two. There is no README at the repo root; docs live in `src-django/README.md` and specs in `openspec/`.

## Rules for agents

These are the rules that matter most. Follow them unless the user says otherwise.

- **Feature slices own their code.** Everything for a feature lives in `apps/<feature>/`: models, views, forms, URLs, query helpers, and its `templates/<feature>/` partials. Don't create a shared service layer for one feature; sharing logic across features requires asking the user first.
- **Keep views thin.** ORM reads live in `apps/<feature>/queries.py`, domain rules in `apps/<feature>/services.py` (or a domain module such as `classification.py`/`sorting.py`), and request parsing/validation in `forms.py`. Queries anywhere else require asking the user first.
- **Merchants are global, not per-user.** `Merchant` and `MerchantAlias` have no user field, and the merchant admin pages read counterparty names across all users' transactions. This is intentional; do not add per-user scoping without asking.
- **The database schema is inherited.** `db_table`/`db_column` names and the existing indexes must keep matching the production PostgreSQL schema, which is adopted once with `migrate --fake-initial`. Never rename tables or columns casually; add a migration instead.
- **The UI and URL contract is shared with the E2E suite.** Don't change URLs, `data-testid` values, element ids, htmx attributes, or user-visible Dutch labels without updating `tests/Budget.E2e`.

## Commands

- Install: `cd src-django && pip install -e ".[dev]"` (use a venv; `.venv` is gitignored)
- Run the app: `python manage.py runserver 5080` — needs a reachable database; reads the repository-level `.env` (gitignored; see `src-django/.env.example`)
- Migrate: `python manage.py migrate`
- Unit tests: `cd src-django && python manage.py test tests` — uses `DATABASE_URL`; point it at SQLite for a scratch run (`DATABASE_URL=sqlite:///budget-e2e.sqlite3`)
- Single test: `python manage.py test tests.test_views.UploadViewTests.test_upload_valid_redirects_to_latest_month`
- Lint: `ruff check config apps tests`
- Type check: `mypy config apps` (strict, via django-stubs)
- Build the solution: `dotnet build Budget.slnx`
- E2E tests: the app MUST be running first, then `dotnet test tests/Budget.E2e/Budget.E2e.csproj`
  - E2E base URL defaults to `http://localhost:5080` (`tests/Budget.E2e/Support/Routes.cs`), override with `PLAYWRIGHT_BASE_URL`
  - Building the E2E project auto-downloads Playwright Chromium (`PlaywrightPlatform=all` in `tests/Directory.Build.props`); a clean run can take a while

## Database

- Connection string resolution order: `DATABASE_URL` → `ConnectionStrings__Budget` (Npgsql `Key=Value;...` form) → `BUDGET_DB_CONNECTION` → hardcoded `budget:budget@localhost:5432/budget`
- Business models are in `apps/transactions/models.py` and `apps/merchants/models.py`; migrations live in `apps/<feature>/migrations` and are created with `python manage.py makemigrations`
- `Transaction` has a unique constraint on `(iban, follow_number, user_id)` and `amount` precision (10,2); uploads dedupe on that key
- Django adds its own auth/contenttypes/sessions tables alongside the three business tables (`transactions`, `merchants`, `merchant_aliases`)

## Auth & test mode

- `config.middleware.BudgetLoginRequiredMiddleware` requires authentication for every non-public request; htmx requests get a bare 401 instead of a login page
- OIDC is optional: if `Oidc__Authority` is empty the app runs cookie-only with no OIDC routes. When set, `mozilla-django-oidc` is mounted under `/oidc/` and the provider `sub` becomes the Django username and transaction `user_id`
- `config.middleware.TestModeAuthMiddleware` is active only when `settings.TEST_MODE` (`DEBUG` and `BUDGET_TEST_MODE`, both environment-driven). It authenticates any request carrying an `X-Test-User` header as that user without touching the database, stores the id in the signed-cookie session, and bypasses CSRF. E2E tests rely on this — each test uses a fresh GUID
- Run the app with `BUDGET_DEBUG=true BUDGET_TEST_MODE=true` to enable test mode

## Architecture

- `config/` holds settings, root URLs, the OIDC auth backend, middleware, health probes, and OpenTelemetry setup
- `apps/budget/` is the monthly overview (`/budget/<year>/<month>/<week-or-iban>`): `views.py`, `services.py` (summary calculation), `sorting.py`, `queries.py`, and templates
- `apps/transactions/` holds the `Transaction` model, `classification.py` (fixed vs. variable rules), `importer.py` (Rabobank CSV), and the upload/toggle-fixed views
- `apps/merchants/` holds `Merchant`/`MerchantAlias`, `normalizer.py`, `queries.py`, and the merchant admin views/forms (rows, options, map, link, clear)
- Templates are Django templates; `templates/base.html` is the shared layout and htmx endpoints return the `_*.html` partials. The frontend is **htmx 2 + Alpine.js + Pico CSS + Bootstrap 5 grid + lucide icons loaded from CDN** (no bundling)
- E2E locators use `GetByTestId` — do not assert on CSS classes, so a redesign doesn't break tests
- Rabobank CSV import: comma-delimited with quoted fields, latin-1 encoded

## Testing

- Django `TestCase`; database tests extend `BudgetDbTestCase` from `tests/base.py` and use `make_transaction(...)` for fixtures
- Test names follow `test_<behavior>` (e.g. `test_upload_valid_redirects_to_latest_month`)
- View tests use `django.test.Client` with `HTTP_X_TEST_USER` and `@override_settings(TEST_MODE=True)`
- Health tests disable test mode; importer/domain tests need no client
- Static checks are strict: ruff line length 100 and `mypy` with the django-stubs plugin

## OpenSpec workflow

This repo is spec-driven. `openspec/` holds `specs/` (accepted capabilities) and `changes/` (active work; archived ones under `changes/archive/`). There are currently no active changes.

- Drive changes through the bundled skills/commands, not ad hoc edits: propose, update, apply, sync, archive, explore (`.opencode/skills/` and `/opsx-*` commands)
- Before implementing, read the change's proposal/design/specs/tasks under `openspec/changes/<name>/`
- `openspec/config.yaml` is placeholder — treat it as empty until filled in

## CI/CD

Pushes to `main` run lint, type checks, Django tests, and the Playwright E2E suite in `.github/workflows/ci.yml`. The workflow installs `src-django` with `pip install -e ".[dev]"`, runs `ruff`, `mypy`, `manage.py migrate` and `manage.py test` against a Postgres service container, then builds `tests/Budget.E2e`, installs Chromium via `playwright.ps1`, starts the app with gunicorn on port 5080 and runs the E2E suite.

On pushes to `main` the workflow then builds and pushes `src-django/Dockerfile` to `images.timo-hermans.nl`. Version format: `YYYY-MM-DD-<run_number>`. The `latest` tag is also updated.
