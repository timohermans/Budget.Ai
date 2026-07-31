## Purpose

Defines the externally observable behavior of the .NET MVC budget application - the overview dashboard, budget calculations, CSV import, transaction toggling, persistence, and formatting - which replaces the Django implementation without changing what users and the e2e suite see.

## ADDED Requirements

### Requirement: Budget overview page

The system SHALL render a budget overview page for a given year and month showing the month name and date range, the monthly budget, income, expenses, spent, and left totals, one summary card per week (week number, left, spent, budget, progress bar), a balance section per IBAN, and expandable transaction lists.

#### Scenario: Empty dashboard

- **WHEN** a user with no transactions opens the overview
- **THEN** the page SHALL render without errors
- **THEN** the current month name SHALL be shown
- **THEN** budget, spent, and left SHALL all be zero

#### Scenario: Month navigation

- **WHEN** a user navigates to a specific year and month
- **THEN** the overview SHALL reflect that month's data
- **WHEN** the user navigates between months
- **THEN** the month display and totals SHALL update accordingly

#### Scenario: Week cards show computed values

- **WHEN** a user with transactions opens the overview
- **THEN** each week card SHALL display budget, spent, and left values and a progress bar reflecting spent relative to budget

#### Scenario: URL week parameter expands a week

- **WHEN** a user navigates to an overview URL that includes a week segment
- **THEN** that week's transaction list SHALL be expanded on page load

#### Scenario: IBAN balance section

- **WHEN** a user has transactions on multiple IBANs
- **THEN** the overview SHALL show each IBAN with its net balance for the month

#### Scenario: Transactions listed per week

- **WHEN** a week is expanded
- **THEN** its transactions SHALL be listed with date, amount, counterparty name, description, and a fixed-status toggle when the transaction is fixed or explicitly flagged

### Requirement: Budget calculation engine

The system SHALL compute the monthly budget from the user's transactions as: fixed income (previous month, main account) minus fixed expenses (previous month, main account); spent as this month's variable expenses on the main account; left as budget minus spent; and each week's budget distributed proportionally to its number of days.

#### Scenario: Baseline budget

- **WHEN** a user has last-month fixed income of 3000 and fixed expenses of 800
- **THEN** budget SHALL equal 2200, income 3000, expenses 800

#### Scenario: Spent includes only variable expenses

- **WHEN** a user has this-month variable expenses totalling 300
- **THEN** spent SHALL equal 300

#### Scenario: Left equals budget minus spent

- **WHEN** a user has a budget and spent total
- **THEN** left SHALL equal budget minus spent

#### Scenario: Week budget proportional to days

- **WHEN** a monthly budget is known
- **THEN** each week SHALL receive budget proportional to its days relative to the month's total days

#### Scenario: Fixed income from previous month

- **WHEN** a user has fixed income transactions in the previous month
- **THEN** they SHALL be included in this month's budget

#### Scenario: Fixed expenses from previous month

- **WHEN** a user has fixed expense transactions in the previous month
- **THEN** they SHALL be included in this month's budget

#### Scenario: Own-account transfers excluded

- **WHEN** a previous-month income transaction comes from an own account
- **THEN** it SHALL NOT count as fixed income and SHALL NOT affect the budget

#### Scenario: PayPal transactions excluded

- **WHEN** a previous-month transaction names a counterparty containing "paypal"
- **THEN** it SHALL NOT count as fixed income or fixed expense

### Requirement: Main account selection

The system SHALL treat the user's IBANs as own accounts and SHALL select the default main account as the user's most frequently used IBAN, using it when no explicit account is selected.

#### Scenario: Most frequent IBAN is default

- **WHEN** a user has transactions across several IBANs and opens the overview without selecting an account
- **THEN** the overview SHALL use the most frequently used IBAN as the main account for income, expenses, and spent

#### Scenario: Explicit IBAN selection

- **WHEN** a user navigates to an overview URL that includes an IBAN segment
- **THEN** the overview SHALL use that IBAN as the main account and expand that IBAN's balance section

### Requirement: CSV transaction import

The system SHALL accept an uploaded Rabobank CSV file - latin-1 encoded, comma-delimited, quoted fields, header row, amounts with comma decimal separators, zero-padded follow numbers - and create the transactions for the authenticated user, ignoring rows that duplicate an existing (IBAN, follow number, user), then redirect with HTTP 302 to the overview of the most recent transaction's month.

#### Scenario: Valid CSV upload

- **WHEN** a user uploads a valid Rabobank CSV
- **THEN** the transactions SHALL be created for that user
- **THEN** the response SHALL be an HTTP 302 redirect to the month of the most recent transaction

#### Scenario: Duplicate upload

- **WHEN** a user uploads a CSV whose rows already exist
- **THEN** no duplicate transactions SHALL be created

#### Scenario: Missing file

- **WHEN** a user submits the upload without a file
- **THEN** no transactions SHALL be created and the upload error response SHALL be returned

#### Scenario: Malformed content

- **WHEN** a user uploads a file that cannot be parsed
- **THEN** no transactions SHALL be created and the upload error response SHALL be returned

### Requirement: Toggle fixed status

The system SHALL toggle a transaction's fixed status on POST, returning the updated toggle control, and SHALL update the page's spent total, left total, and the affected week's spent, left, and progress bar values in a single response.

#### Scenario: Fixed to variable

- **WHEN** a user toggles a fixed transaction
- **THEN** spent SHALL increase by the transaction's absolute amount and left SHALL decrease by the same amount
- **THEN** the week's spent, left, and progress bar SHALL update accordingly
- **THEN** the toggle control SHALL render in its new state

#### Scenario: Variable to fixed

- **WHEN** a user toggles a variable transaction back
- **THEN** spent SHALL decrease by the transaction's absolute amount and left SHALL increase by the same amount
- **THEN** the week's spent, left, and progress bar SHALL update accordingly
- **THEN** the toggle control SHALL render in its new state

#### Scenario: Repeated toggling

- **WHEN** a user toggles the same transaction repeatedly
- **THEN** each toggle SHALL succeed and flip the state back and forth correctly

### Requirement: Transaction classification

The system SHALL classify a transaction as fixed or variable: variable when it is explicitly flagged, is income from an own account, or the counterparty name contains "paypal"; fixed when the code is "db" and the description contains "sparen", the code is "db" and the counterparty is "Rabobank", or the code is one of "sb", "cb", "bg", "ei", "tb". An amount of zero or more is income; a negative amount is an expense.

#### Scenario: Explicitly flagged transactions are variable

- **WHEN** a transaction is explicitly flagged as not fixed
- **THEN** it SHALL be classified variable

#### Scenario: Own-account income is variable

- **WHEN** an income transaction's counterparty is an own account
- **THEN** it SHALL be classified variable

#### Scenario: PayPal counterparties are variable

- **WHEN** a transaction's counterparty name contains "paypal"
- **THEN** it SHALL be classified variable

#### Scenario: Savings-related codes are fixed

- **WHEN** a transaction has code "db" and its description contains "sparen" or its counterparty is "Rabobank"
- **THEN** it SHALL be classified fixed

#### Scenario: Known fixed codes

- **WHEN** a transaction has code "sb", "cb", "bg", "ei", or "tb"
- **THEN** it SHALL be classified fixed

### Requirement: Per-user data isolation

The system SHALL store each transaction with the authenticated user's id and SHALL only read and write the current user's transactions. A transaction is uniquely identified by its IBAN, follow number, and user.

#### Scenario: User data isolation

- **WHEN** one user has uploaded transactions
- **THEN** those transactions SHALL only be visible to that user
- **THEN** other users SHALL NOT see them in their overview or calculations

#### Scenario: Unique transactions

- **WHEN** two rows have the same IBAN, follow number, and user
- **THEN** only one transaction SHALL be stored

### Requirement: Formatting contract

The system SHALL render numbers and dates in the formats the e2e suite parses: progress bar value and max attributes with a decimal point, English month names in the month display, and dates as day-month.

#### Scenario: Progress bar attributes

- **WHEN** a progress bar is rendered
- **THEN** its value and max attributes SHALL use a decimal point as the separator

#### Scenario: Month display

- **WHEN** the month display is rendered
- **THEN** the month name SHALL be the English month name

#### Scenario: Transaction dates

- **WHEN** a transaction date is rendered
- **THEN** it SHALL use the day-month format (e.g. "03-01")

### Requirement: Production authentication

The system SHALL authenticate users through the existing OpenID Connect provider and SHALL derive the transaction user id from the provider's subject claim.

#### Scenario: Unauthenticated request

- **WHEN** an unauthenticated request is made to a protected page
- **THEN** the system SHALL redirect to the OpenID Connect provider for login

#### Scenario: Authenticated request

- **WHEN** a request is authenticated via the provider
- **THEN** the provider's subject SHALL be used as the transaction user id
