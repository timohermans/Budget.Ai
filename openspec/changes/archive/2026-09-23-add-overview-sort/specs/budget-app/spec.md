## ADDED Requirements

### Requirement: Overview transaction sorting

The system SHALL render a sort control below the summary card on the budget overview that determines the order of the transaction lists in the expanded week section and the expanded IBAN section. The selected sort SHALL be reflected in the URL via a `sort` query parameter and SHALL be applied server-side. The supported sort orders SHALL be: date descending (default), date ascending, biggest expenses first, smallest expenses first, counterparty display name A to Z, and counterparty display name Z to A. The fixed-transaction detail card SHALL NOT be affected by the selected sort.

#### Scenario: Default sort is date descending

- **WHEN** a user opens the overview without a `sort` query parameter
- **THEN** the expanded transaction lists SHALL be ordered by date descending
- **THEN** the sort control SHALL show "datum (eind - begin)" as selected

#### Scenario: Sort options and labels

- **WHEN** the sort control is rendered
- **THEN** it SHALL offer exactly the options "datum (eind - begin)", "datum (begin - eind)", "hoogste uitgaves", "kleinste uitgaves", "winkel (a - z)", "winkel (z - a)"
- **THEN** each option SHALL map to its sort order: date descending, date ascending, biggest expenses first, smallest expenses first, counterparty name A to Z, counterparty name Z to A

#### Scenario: Sort by biggest expenses first

- **WHEN** a user selects "hoogste uitgaves"
- **THEN** the expanded transaction lists SHALL be ordered from the most negative amount to the most positive amount
- **THEN** income transactions SHALL appear after all expense transactions

#### Scenario: Sort by smallest expenses first

- **WHEN** a user selects "kleinste uitgaves"
- **THEN** expenses SHALL be ordered from the smallest to the largest absolute amount
- **THEN** income transactions SHALL appear after all expense transactions

#### Scenario: Sort by counterparty name

- **WHEN** a user selects "winkel (a - z)"
- **THEN** the expanded transaction lists SHALL be ordered by the displayed counterparty name alphabetically, ignoring case
- **WHEN** a user selects "winkel (z - a)"
- **THEN** the expanded transaction lists SHALL be ordered in the reverse alphabetical order

#### Scenario: Ties break by date descending

- **WHEN** multiple transactions have an equal sort key (same date, same amount, or same counterparty name)
- **THEN** they SHALL be ordered by date descending within the equal group

#### Scenario: Sort applies to expanded sections

- **WHEN** a week section is expanded
- **THEN** its transaction list SHALL be ordered by the selected sort
- **WHEN** an IBAN section is expanded
- **THEN** its transaction list SHALL be ordered by the selected sort

#### Scenario: Selecting a sort updates the page without a full reload

- **WHEN** a user changes the sort control while a week or IBAN section is expanded
- **THEN** the page SHALL update to show the selected sort order without a full page reload
- **THEN** the browser URL SHALL reflect the selected sort
- **THEN** the previously expanded week or IBAN section SHALL remain expanded

#### Scenario: Sort is URL-representable

- **WHEN** a user opens an overview URL containing a valid `sort` query parameter
- **THEN** the overview SHALL render with that sort order applied
- **WHEN** the `sort` query parameter is missing or invalid
- **THEN** the overview SHALL render with the default sort order

#### Scenario: Sort survives navigation

- **WHEN** a user has selected a sort order and then navigates via month navigation, week expansion, or IBAN expansion
- **THEN** the target URL SHALL carry the selected sort
- **THEN** the rendered page SHALL keep the selected sort order

#### Scenario: Fixed detail card is unaffected

- **WHEN** a user has selected a sort order and opens the fixed-transaction detail card
- **THEN** the detail card SHALL NOT be ordered by the selected sort