# AGENTS.md

Budget app, migrated from Django to .NET. ASP.NET Core MVC (net10.0) monolith in `src/Budget.Web`; MSTest v4 unit tests in `tests/Budget.Tests`; MSTest + Playwright E2E in `tests/Budget.E2e`. Solution file is `Budget.slnx` (new XML format). No README; specs live in `openspec/`.

## Rules for agents

These are the rules that matter most. Follow them unless the user says otherwise.

- **Do not create repositories.** `BudgetDbContext` is the repository. Inject it directly into controllers and query via EF Core.
- **Vertical slice style:** keep logic inside controllers and do not share logic between features. Don't create private methods unless the logic is actually shared. Sharing within a single feature folder is allowed (e.g. `Features/Merchants/` query helpers). Sharing across features requires asking the user first.
- **One controller action per controller class.** Each action gets its own controller file. View models used only by that action may live in the same file.
- **Database EF queries belong at the edges of controller actions**, i.e. loading inputs at the start and persisting at the end. Queries anywhere else (a helper, a domain class, a loop) require asking the user first.
- **Merchants are global, not per-user.** `Merchant` and `MerchantAlias` have no `UserId`, and the merchant admin pages read counterparty names across all users' transactions. This is intentional; do not add per-user scoping without asking.

## Commands

- Build: `dotnet build Budget.slnx`
- Unit tests: `dotnet test tests/Budget.Tests/Budget.Tests.csproj` (uses EF Core InMemory, no DB needed)
- Single unit test class: `dotnet test tests/Budget.Tests/Budget.Tests.csproj --filter "FullyQualifiedName~ToggleFixedControllerTests"`
- Run the app: `dotnet run --project src/Budget.Web` — needs local Postgres, loads `.env` via `dotenv.net` (gitignored; see `.env.example`)
- E2E tests: the app MUST be running first, then `dotnet test tests/Budget.E2e/Budget.E2e.csproj`
  - E2E base URL defaults to `http://localhost:5080` (`tests/Budget.E2e/Support/Routes.cs`), override with `PLAYWRIGHT_BASE_URL`
  - Building the E2E project auto-downloads Playwright Chromium (`PlaywrightPlatform=all` in `tests/Directory.Build.props`); a clean run can take a while
- EF migrations: `dotnet ef database update --project src/Budget.Web` (requires `dotnet-ef` global tool)

## Database

- PostgreSQL 18, EF Core 10 + Npgsql, **snake_case naming convention** via `EFCore.NamingConventions`
- Connection string resolution order: `ConnectionStrings__Budget` env → `BUDGET_DB_CONNECTION` env (legacy, slated for removal) → hardcoded `Host=localhost;Database=budget;Username=budget;Password=budget`
- Migrations live in `src/Budget.Web/Migrations`; create them with `dotnet ef migrations add <Name> --project src/Budget.Web`
- `Transaction` has a unique index on `(Iban, FollowNumber, UserId)` and `Amount` precision (10,2); uploads dedupe on that key

## Auth & test mode

- Fallback authorization policy requires an authenticated user on every request
- OIDC is optional: if `Oidc:Authority` is empty the app uses cookie-only auth (good for local dev)
- `TestModeAuthMiddleware` (registered only in Development) authenticates any request carrying an `X-Test-User` header as that user, bypassing OIDC and antiforgery. E2E tests rely on this — each test uses a fresh GUID
- Run the app with `ASPNETCORE_ENVIRONMENT=Development` to enable test mode

## Architecture

- Feature folders under `src/Budget.Web/Features/` (`Budget/`, `Transactions/`, `Merchants/`) hold controllers. Domain logic lives in `src/Budget.Web/Domain/` (e.g. `Transactions/SummaryCalculator`, `Transactions/TransactionClassifier`). The Rabobank CSV importer lives in `Features/Transactions/RabobankCsvImporter.cs`. `Data/BudgetDbContext.cs` is the EF context.
- Views are MVC Razor with **htmx 2 + Alpine.js + Pico CSS + lucide icons loaded from CDN** (no bundling). Interactive rows (toggle-fixed, merchant admin) use `hx-post` + OOB swaps returning partials.
- E2E locators use `GetByTestId` — do not assert on CSS classes, so a redesign doesn't break tests.
- Rabobank CSV import: comma-delimited with quoted fields, latin-1 encoded. Culture is forced to InvariantCulture in `Program.cs`.

## Testing

- MSTest with `[TestClass]` / `[TestMethod]`. Test names follow `Method_WhenCondition_ThenResult`.
- Database tests use EF Core InMemory with a fresh database per test (`UseInMemoryDatabase(Guid.NewGuid().ToString())`).
- Loggers in tests use `NullLogger<T>.Instance`.
- Controller tests set `ControllerContext` with a `ClaimsPrincipal` carrying a `sub` claim (the user id).
- The build currently has MSTEST0037 analyzer warnings in the E2E project. These are pre-existing.

## OpenSpec workflow

This repo is spec-driven. `openspec/` holds `specs/` (accepted capabilities) and `changes/` (active work; archived ones under `changes/archive/`). There are currently no active changes.

- Drive changes through the bundled skills/commands, not ad hoc edits: propose, update, apply, sync, archive, explore (`.opencode/skills/` and `/opsx-*` commands)
- Before implementing, read the change's proposal/design/specs/tasks under `openspec/changes/<name>/`
- `openspec/config.yaml` is placeholder — treat it as empty until filled in

## CI/CD

Pushes to `main` run unit and E2E tests in `.github/workflows/ci.yml` (against a Postgres service container and a prebuilt Playwright container). The workflow then builds and pushes a Docker image to `registry.timo-hermans.nl` and bumps the image tag in the separate `timohermans/Cluster` GitOps repo via kustomize. Version format: `YYYY-MM-DD-<run_number>`. The `latest` tag is also updated.
