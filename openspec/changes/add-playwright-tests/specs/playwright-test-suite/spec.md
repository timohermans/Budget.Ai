## Purpose

Provides a .NET Playwright E2E test suite that covers all user-facing functionality of the budget application, enabling safe refactoring and migration.

## ADDED Requirements

### Requirement: Test project structure

The test suite SHALL be a .NET MSTest v4 project using the Microsoft.Playwright library, organized into page object models, support utilities, and test classes.

#### Scenario: Project compiles and runs

- **WHEN** the .NET project is built
- **THEN** it SHALL compile without errors
- **WHEN** tests are executed
- **THEN** they SHALL run against a Chromium browser

#### Scenario: Test project structure

- **WHEN** the project is laid out
- **THEN** it SHALL contain separate directories for Pages, Support, and Tests
- **THEN** it SHALL contain a Routes class for URL constants
- **THEN** it SHALL contain a PlaywrightFixture for browser lifecycle management

### Requirement: Per-test user isolation

Each test SHALL use a unique GUID as the `X-Test-User` header value, ensuring complete data isolation between tests running in parallel.

#### Scenario: Test sets unique user header

- **WHEN** a test initializes
- **THEN** it SHALL generate a new GUID
- **THEN** it SHALL set `X-Test-User` as an extra HTTP header on the page
- **THEN** no other test SHALL use the same GUID concurrently

#### Scenario: Parallel execution safety

- **WHEN** multiple tests run in parallel
- **THEN** each test SHALL have its own IPage instance
- **THEN** each test SHALL have its own user GUID
- **THEN** no test SHALL be affected by another test's data

### Requirement: Budget dashboard tests

The suite SHALL test the budget dashboard rendering, navigation, and display of weekly summaries, progress bars, IBAN balances, and transaction lists.

#### Scenario: Empty dashboard shows appropriate state

- **WHEN** a test user with no transactions navigates to the budget dashboard
- **THEN** the page SHALL render without errors
- **THEN** the page SHALL show the current month header

#### Scenario: Dashboard defaults to current month

- **WHEN** a test user navigates to `/budget/`
- **THEN** the URL SHALL resolve to `/budget/<current-year>/<current-month>/`

#### Scenario: Month navigation changes data

- **WHEN** a test user uploads transactions for known months
- **THEN** navigating to each month SHALL show that month's data
- **THEN** month navigation buttons SHALL be functional

#### Scenario: Week card shows correct values

- **WHEN** a test user uploads transactions with known amounts
- **THEN** each week card SHALL display the correct budget, spent, and left values
- **THEN** the progress bar SHALL reflect the correct percentage

#### Scenario: Week expansion shows transactions

- **WHEN** a test user clicks a week card header
- **THEN** the week SHALL expand to show transactions
- **THEN** each transaction SHALL display amount, counterparty name, and toggle button

#### Scenario: IBAN balance card shows correct balance

- **WHEN** a test user has transactions on multiple IBANs
- **THEN** the IBAN balance section SHALL show each IBAN with its calculated balance

#### Scenario: URL week parameter expands correct week

- **WHEN** a test user navigates to `/budget/<year>/<month>/<week>`
- **THEN** the specified week SHALL be expanded on page load

### Requirement: CSV upload tests

The suite SHALL test the file upload flow, including successful imports, duplicate handling, and error conditions.

#### Scenario: Valid CSV uploads successfully

- **WHEN** a test user uploads a valid Rabobank CSV file
- **THEN** the upload SHALL succeed
- **THEN** the user SHALL be redirected to the budget page for the most recent transaction's month
- **THEN** the uploaded transactions SHALL appear in the dashboard

#### Scenario: Duplicate CSV does not create duplicates

- **WHEN** a test user uploads the same CSV file twice
- **THEN** the second upload SHALL succeed without error
- **THEN** no duplicate transactions SHALL appear in the dashboard

#### Scenario: Malformed CSV shows error

- **WHEN** a test user uploads a malformed file (invalid format, wrong encoding)
- **THEN** the system SHALL display an inline error message in Dutch

#### Scenario: Missing file shows error

- **WHEN** a test user submits the upload form without a file
- **THEN** the system SHALL display an inline error message

### Requirement: Toggle fixed tests

The suite SHALL test the HTMX-powered toggle-fixed functionality and its OOB swap updates.

#### Scenario: Toggle variable to fixed

- **WHEN** a test user clicks the toggle button on a variable transaction
- **THEN** the transaction SHALL be marked as fixed
- **THEN** the spent total SHALL decrease by the transaction amount
- **THEN** the left total SHALL increase by the transaction amount

#### Scenario: Toggle fixed to variable

- **WHEN** a test user clicks the toggle button on a fixed transaction
- **THEN** the transaction SHALL be marked as variable
- **THEN** the spent total SHALL increase by the transaction amount
- **THEN** the left total SHALL decrease by the transaction amount

#### Scenario: OOB swaps update all targets

- **WHEN** a test user toggles a transaction's fixed status
- **THEN** the spent total element SHALL be updated via OOB swap
- **THEN** the left total element SHALL be updated via OOB swap
- **THEN** the progress bar SHALL be updated via OOB swap
- **THEN** the week-specific spent and left SHALL be updated

#### Scenario: Multiple toggles work repeatedly

- **WHEN** a test user toggles a transaction multiple times
- **THEN** each toggle SHALL succeed
- **THEN** the state SHALL flip back and forth correctly each time

### Requirement: Budget calculation tests

The suite SHALL verify the budget calculation engine produces correct values for various transaction compositions.

#### Scenario: Baseline budget calculation

- **WHEN** a test user has last-month fixed income of €3000 and fixed expenses of €800
- **THEN** the displayed budget SHALL equal €2200

#### Scenario: Spent includes only variable expenses

- **WHEN** a test user has this-month variable expenses totaling €500
- **THEN** the spent total SHALL equal €500

#### Scenario: Budget minus spent equals left

- **WHEN** a test user has budget of €2200 and spent of €500
- **THEN** the left amount SHALL equal €1700

#### Scenario: Own-account transfers excluded from fixed

- **WHEN** a test user has a last-month transfer to their own savings account
- **THEN** that transfer SHALL NOT be counted as a fixed expense
- **THEN** it SHALL NOT affect the budget calculation

#### Scenario: PayPal transactions excluded from fixed

- **WHEN** a test user has a last-month PayPal transaction with a fixed-income code
- **THEN** that transaction SHALL NOT be counted as fixed

#### Scenario: Week budget distributed proportionally

- **WHEN** a test user has a monthly budget of €2200 spread across weeks
- **THEN** each week SHALL receive a budget proportional to its number of days relative to the month total

### Requirement: CsvBuilder utility

The suite SHALL include a CsvBuilder utility class that generates valid Rabobank-format CSV strings for test data setup.

#### Scenario: CsvBuilder generates valid CSV

- **WHEN** CsvBuilder is given transaction data
- **THEN** it SHALL produce a valid semicolon-delimited CSV string
- **THEN** the CSV SHALL have the correct Rabobank column headers
- **THEN** the CSV SHALL be encodable as latin-1

#### Scenario: CsvBuilder handles multiple transactions

- **WHEN** CsvBuilder is given multiple transactions
- **THEN** each transaction SHALL appear as a separate CSV row
- **THEN** decimal amounts SHALL use comma as decimal separator

#### Scenario: CsvBuilder configures date ranges

- **WHEN** CsvBuilder is given transactions spanning multiple months
- **THEN** the generated CSV SHALL include all specified dates correctly

### Requirement: Routes class

The test suite SHALL include a Routes class that centralizes all URL paths, making them easy to reconfigure when migrating to .NET Razor Pages.

#### Scenario: Routes expose all app URLs

- **WHEN** the Routes class is referenced
- **THEN** it SHALL provide methods for the budget dashboard, upload, and toggle-fixed endpoints
- **THEN** it SHALL accept year/month/week/iban parameters where applicable

#### Scenario: Routes base URL is configurable

- **WHEN** the test suite starts
- **THEN** the Routes SHALL use a configurable BaseUrl (defaulting to `http://localhost:8000`)
