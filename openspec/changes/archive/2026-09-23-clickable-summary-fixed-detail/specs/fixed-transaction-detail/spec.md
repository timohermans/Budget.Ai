## Purpose

Lets users click the fixed income and expense summary values in the budget overview to see a detail card listing all fixed transactions of that type from the previous month (the same transactions that contribute to the summary totals), with the card state represented in the URL.

## ADDED Requirements

### Requirement: Summary income and expense values are clickable
The system SHALL render the income and expense values in the budget summary as clickable elements that users can activate to view fixed transaction details.

#### Scenario: Clicking income value shows fixed income card
- **WHEN** a user clicks the income value in the summary card
- **THEN** a detail card listing all fixed income transactions from the previous month appears below the summary
- **THEN** the browser URL includes `?fixed=income`

#### Scenario: Clicking expenses value shows fixed expenses card
- **WHEN** a user clicks the expenses value in the summary card
- **THEN** a detail card listing all fixed expense transactions from the previous month appears below the summary
- **THEN** the browser URL includes `?fixed=expenses`

### Requirement: Fixed transaction detail card shows the transactions that contribute to the summary totals
The system SHALL display the same fixed transactions that were used to compute the income and expense summary totals in the detail card.

#### Scenario: Detail card shows last month's fixed income transactions
- **WHEN** the user clicks the income value
- **THEN** the detail card shows the fixed income transactions from the previous month that were used to compute the income total

#### Scenario: Detail card shows last month's fixed expense transactions
- **WHEN** the user clicks the expenses value
- **THEN** the detail card shows the fixed expense transactions from the previous month that were used to compute the expenses total

### Requirement: Fixed transaction detail card can be dismissed
The system SHALL allow users to dismiss the detail card, removing it from the view and clearing the query parameter from the URL.

#### Scenario: Closing the detail card removes it from the view
- **WHEN** a user clicks the close button on the detail card
- **THEN** the detail card is removed from the page
- **THEN** the URL no longer contains the `fixed` query parameter

#### Scenario: Navigating to a different month dismisses the detail card
- **WHEN** a user navigates to a different month using the previous/next navigation
- **THEN** the detail card is removed from the page if it was open