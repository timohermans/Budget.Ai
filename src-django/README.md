# Budget — Django

Django 6.1 port of the ASP.NET Core `Budget.Web` application. It implements the
same budget overview, CSV import, fixed-transaction toggling, merchant logo map,
sorting, and authentication behavior. It adopts the existing PostgreSQL schema
created by the .NET app; after that one-time adoption Django owns the schema
through migrations.

The existing .NET Playwright suite passes unchanged against this app. During the
port, all 42 E2E tests were run against Django on `http://localhost:5080` and
against the Docker image.

## Requirements

- Python 3.12+ (tested with 3.14)
- PostgreSQL 18 for real data, or SQLite for a scratch local database
- The .NET E2E project if you want to run the Playwright suite

Runtime dependencies are pinned in `pyproject.toml`:

- Django 6.1.2
- psycopg 3
- mozilla-django-oidc 5
- python-dotenv, dj-database-url, gunicorn
- django-stubs/mypy/ruff as development extras

## Setup

```bash
cd src-django
python3 -m venv .venv
source .venv/bin/activate
pip install -e ".[dev]"
```

The app reads the repository-level `.env` (the same file the .NET app uses) and
also accepts environment variables. The relevant keys are:

```dotenv
ConnectionStrings__Budget=Host=localhost;Database=budget;Username=budget;Password=budget
# or: DATABASE_URL=postgres://budget:budget@localhost:5432/budget
# or: DATABASE_URL=sqlite:///budget.sqlite3

Oidc__Authority=
Oidc__ClientId=
Oidc__ClientSecret=

OpenTelemetry__Endpoint=
OpenTelemetry__Headers=

BUDGET_DEBUG=true
BUDGET_TEST_MODE=true
BUDGET_ALLOWED_HOSTS=localhost,127.0.0.1
# BUDGET_SECRET_KEY=change-me
```

`DATABASE_URL` takes precedence, then `ConnectionStrings__Budget` (Npgsql
`Key=Value;...` form), then `BUDGET_DB_CONNECTION`, then the local default
`budget:budget@localhost:5432/budget`.

## Database schema and existing data

The business models are now managed by Django. Their migrations live in
`apps/transactions/migrations` and `apps/merchants/migrations`.

On a new or empty database:

```bash
python manage.py migrate
```

This creates Django's auth/content-type/session tables and the three business
tables (`transactions`, `merchants`, `merchant_aliases`).

### Adopting an existing EF Core database (one time)

Do not recreate the production database. Back it up, test the adoption against a
clone, then mark the baseline migrations as applied:

```bash
pg_dump -Fc budget > budget-before-django-migrations.dump

python3 manage.py migrate auth
python3 manage.py migrate sessions
python3 manage.py migrate --fake-initial
```

`--fake-initial` records the initial business migrations as applied without
recreating the existing tables. Review the plan first:

```bash
python manage.py migrate --plan
python manage.py sqlmigrate transactions 0001
```

After adoption, normal `python manage.py migrate` applies future schema changes.
`inspectdb` can still be used to compare a database with the models:

```bash
python manage.py inspectdb transactions merchants merchant_aliases
```

but the committed migrations are the source of truth now.

## Scratch SQLite database (local only)

For a self-contained local run without PostgreSQL:

```bash
export DATABASE_URL=sqlite:///budget.sqlite3
export BUDGET_DEBUG=true
export BUDGET_TEST_MODE=true

python manage.py migrate
python manage.py runserver 5080
```

## Run

```bash
python manage.py runserver 5080
```

URLs:

- `/budget/` — current-month overview
- `/budget/<year>/<month>` — specific month
- `/budget/<year>/<month>/<week-or-iban>` — expanded week or IBAN
- `/transactions/upload` — Rabobank CSV upload
- `/transactions/toggle-fixed` — htmx toggle
- `/merchants` — merchant administration
- `/health/live`, `/health/ready`

## Authentication

The behavior mirrors the .NET app:

- **OIDC mode** — set `Oidc__Authority` (a Keycloak realm URL). The app includes
  `mozilla_django_oidc.urls` under `/oidc/`, maps the provider `sub` claim to the
  Django username, and uses that same value as the transaction `user_id`.
- **Cookie-only mode** — leave `Oidc__Authority` empty. No OIDC routes are
  registered. Local development uses test mode.
- **Test mode** — enabled only when `BUDGET_DEBUG=true` and
  `BUDGET_TEST_MODE=true`. A request with `X-Test-User: <id>` is authenticated as
  that id without creating or looking up a user row, the id is stored in the
  signed-cookie session, and antiforgery validation is bypassed. This is what the
  Playwright suite uses.
- Unauthenticated browser requests redirect to OIDC; unauthenticated htmx
  requests return HTTP 401.

Sessions use Django's signed-cookie backend with an eight-hour lifetime, so the
test-mode session does not need a database table.

## OpenTelemetry

Tracing and log export are configured from the same environment variables as the
.NET app, so one `.env` serves both:

| Variable | Purpose |
| --- | --- |
| `OpenTelemetry__Endpoint` | OTLP/HTTP logs endpoint (e.g. `http://collector:4318/v1/logs`) |
| `OpenTelemetry__Headers` | Collector auth header, e.g. `Authorization=Basic <base64>` |
| `OTEL_SERVICE_NAME` | Resource `service.name` (default `budget-django`) |
| `OTEL_ENVIRONMENT` | Resource `deployment.environment.name` (defaults to `BUDGET_ENV` or the debug state) |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Base OTLP endpoint; `/v1/traces` and `/v1/logs` are appended |
| `OTEL_EXPORTER_OTLP_TRACES_ENDPOINT` | Explicit traces endpoint, overrides the base endpoint |
| `OTEL_EXPORTER_OTLP_LOGS_ENDPOINT` | Explicit logs endpoint, overrides `OpenTelemetry__Endpoint` |
| `OTEL_TRACES_EXPORTER` / `OTEL_LOGS_EXPORTER` | Set to `none` to disable that signal |
| `OTEL_TRACES_SAMPLER_ARG` | Trace sample ratio, `0.0`–`1.0` (default `1.0`) |

Telemetry is off while no endpoint is configured. When enabled, the app exports
spans for HTTP requests (Django middleware) and database queries (psycopg), and
ships the existing console log records over OTLP with trace context attached.
`config/telemetry.py` is invoked from `config/wsgi.py` and `config/asgi.py`
before the Django handler is created, which is required for the tracing
middleware to be registered.

Log records are exported through `config.telemetry.BudgetLoggingHandler`, which
drops record attributes that OTel cannot represent. Django's `django.server`
access logger passes the raw socket as `extra={"request": ...}`; exporting it
would make the SDK emit a warning through the same handler and trip its
recursion guard. `opentelemetry.*` loggers are also kept off the OTLP handler
for the same reason.

## UI

The templates intentionally preserve the existing UI contract: htmx 2,
Alpine.js, Pico CSS, the Bootstrap 5 grid, and lucide icons from CDNs. All
`data-testid` values, element ids, htmx attributes, and Dutch labels used by the
Playwright page objects are present.

## Tests and type checking

Django tests (domain rules, importer, summary, sorting, merchant queries, and
views):

```bash
export DATABASE_URL=sqlite:///budget-e2e.sqlite3
export BUDGET_DEBUG=true
python manage.py test tests
```

Static checks:

```bash
mypy config apps
ruff check config apps tests
```

## Playwright E2E (unchanged .NET suite)

Start Django on port 5080 with test mode, then run the existing suite from the
repository root:

```bash
cd src-django
export DATABASE_URL=sqlite:///budget-e2e.sqlite3   # or the real PostgreSQL URL
export BUDGET_DEBUG=true
export BUDGET_TEST_MODE=true
python manage.py migrate
python manage.py runserver 5080
```

```bash
cd ..
PLAYWRIGHT_BASE_URL=http://localhost:5080 \
  dotnet test tests/Budget.E2e/Budget.E2e.csproj -c Release
```

Each test uses a fresh GUID in `X-Test-User`, so test data is isolated.

## Docker

```bash
docker build -t budget-django src-django
docker run --rm -p 8080:8080 \
  -e DATABASE_URL=postgres://budget:budget@host.docker.internal:5432/budget \
  -e BUDGET_DEBUG=false \
  -e BUDGET_SECRET_KEY=replace-me \
  -e BUDGET_ALLOWED_HOSTS=budget.example.com \
  -e OpenTelemetry__Endpoint=http://collector:4318/v1/logs \
  -e OpenTelemetry__Headers="Authorization=Basic <base64>" \
  budget-django
```

Run `python manage.py migrate` once against the target database before starting
the container. The image runs gunicorn on port 8080.

## Layout

```
src-django/
├── config/                 # settings, root URLs, auth backend, middleware, health
├── apps/
│   ├── budget/             # overview view, summary calculator, sorting, templates
│   ├── transactions/       # Transaction model, classifier, CSV importer, upload/toggle
│   └── merchants/          # Merchant/Alias models, normalizer, queries, admin views
├── templates/base.html     # shared htmx/Alpine/Pico/Bootstrap layout
└── tests/                  # Django unit and view tests
```

## Known constraints

- An existing production database must be adopted once with
  `migrate --fake-initial`. Verify the plan and test against a restored clone
  first.
- The SQLite path is for local testing; production and real E2E runs use
  PostgreSQL.
- Django adds its own auth/content-type/session tables to the shared database.
  The .NET app ignores them.

### Migration path DB from EF to Django ORM managed (once)

- Restore database to the new database (via UI tool)
- `python3 manage.py migrate auth`
- `python3 manage.py migrate sessions`
- `python3 manage.py migrate --fake-initial`
