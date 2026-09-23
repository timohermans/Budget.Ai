## MODIFIED Requirements

### Requirement: Budget overview page

The system SHALL render a budget overview page for a given year and month showing the month name and date range, the monthly budget, income, expenses, spent, and left totals, one summary card per week (week number, left, spent, budget, progress bar), a balance section per IBAN, and expandable transaction lists. The income and expense values in the summary SHALL be clickable, and clicking them SHALL display a fixed-transaction detail card for the selected type. The detail card state SHALL be reflected in the URL via a `fixed` query parameter.

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

#### Scenario: Overview renders with clickable income and expense values

- **WHEN** a user opens the budget overview
- **THEN** the income and expense values in the summary card are rendered as clickable elements
- **THEN** clicking the income value fetches and displays a fixed income detail card
- **THEN** clicking the expense value fetches and displays a fixed expense detail card

#### Scenario: Detail card state is URL-representable

- **WHEN** a user clicks the income value
- **THEN** the URL includes `?fixed=income`
- **WHEN** the user shares or bookmarks that URL
- **THEN** opening it renders the overview with the fixed income detail card visible

#### Scenario: Navigating away dismisses the detail card

- **WHEN** a user has a fixed detail card open and navigates to a different month
- **THEN** the detail card is removed from the page
- **THEN** the URL no longer contains the `fixed` query parameter