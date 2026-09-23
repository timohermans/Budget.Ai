## Purpose

Shows a recognizable circular logo for each transaction's counterparty on the budget overview, backed by a user-curated global merchant logo map maintained through an admin page.

## ADDED Requirements

### Requirement: Merchant logo map

The system SHALL maintain a global merchant map, keyed by normalized counterparty name, where each canonical entry holds an optional display name and either a logo URL or an explicit "no logo" decision, and where distinct counterparty names can be linked to a canonical entry so they resolve to that entry. The map SHALL NOT be scoped to a user and SHALL be shared across all users. CSV import SHALL NOT write to the map.

#### Scenario: Mapped merchant has a logo

- **WHEN** a merchant entry exists with a logo URL
- **THEN** transactions whose counterparty name normalizes to that merchant SHALL display the logo

#### Scenario: Explicit no-logo decision

- **WHEN** a merchant entry exists with the "no logo" decision
- **THEN** transactions whose counterparty name normalizes to that merchant SHALL display the placeholder and SHALL NOT be offered as unmapped

#### Scenario: Unknown merchant

- **WHEN** no merchant entry exists for a counterparty name
- **THEN** transactions with that name SHALL display the placeholder

#### Scenario: Map not written on import

- **WHEN** a CSV file is uploaded containing counterparty names
- **THEN** the upload SHALL create only transactions and SHALL NOT create or modify merchant entries

### Requirement: Counterparty name normalization

The system SHALL map a transaction's counterparty name to a merchant key by trimming surrounding whitespace, lowercasing, collapsing internal runs of whitespace to a single space, and normalizing spacing around hyphens. The same normalization SHALL be applied when a key is stored and when an incoming transaction name is resolved, so the two can never drift apart.

#### Scenario: Case-insensitive resolution

- **WHEN** two transactions name the counterparty "Albert Heijn" and "ALBERT HEIJN"
- **THEN** both SHALL resolve to the same merchant entry

#### Scenario: Whitespace-insensitive resolution

- **WHEN** a transaction names a counterparty with leading, trailing, or repeated internal whitespace
- **THEN** it SHALL resolve to the same merchant entry as the trimmed name

#### Scenario: Hyphen spacing is normalized

- **WHEN** two transactions name the counterparty "AH- Jan Linders 4181" and "AH - Jan Linders 4181"
- **THEN** both SHALL resolve to the same merchant key

#### Scenario: Normalized name stored at import

- **WHEN** a CSV file is imported
- **THEN** each created transaction SHALL store the normalized form of its counterparty name, computed by the same normalizer used for merchant keys

### Requirement: Logo display on overview transactions

The system SHALL render a small circular image to the left of each transaction on the budget overview showing the counterparty's logo when the merchant is mapped, and a placeholder otherwise. When the logo image fails to load, the placeholder SHALL be shown instead.

#### Scenario: Mapped transaction shows logo

- **WHEN** a transaction's counterparty is mapped to a logo URL and a week or IBAN section is expanded
- **THEN** the transaction row SHALL render a circular image with that logo URL

#### Scenario: Unmapped transaction shows placeholder

- **WHEN** a transaction's counterparty has no mapped logo
- **THEN** the transaction row SHALL render the placeholder in place of a logo

#### Scenario: Broken logo falls back to placeholder

- **WHEN** a mapped logo URL cannot be loaded by the browser
- **THEN** the placeholder SHALL be shown in place of the logo

#### Scenario: Linked name shows merchant logo

- **WHEN** a transaction's counterparty name is linked to a merchant with a logo URL and a week or IBAN section is expanded
- **THEN** the transaction row SHALL render that merchant's circular logo

### Requirement: Canonical merchant name in transaction rows

The system SHALL display a mapped merchant's canonical display name instead of the raw counterparty name in a transaction row when the merchant entry has a display name. When the merchant entry has no display name, the raw counterparty name SHALL be shown.

#### Scenario: Mapped merchant with display name

- **WHEN** a transaction's counterparty resolves to a merchant entry with a display name
- **THEN** the transaction row SHALL show the display name instead of the raw counterparty name

#### Scenario: Mapped merchant without display name

- **WHEN** a transaction's counterparty resolves to a merchant entry without a display name
- **THEN** the transaction row SHALL show the raw counterparty name

#### Scenario: Unmapped counterparty

- **WHEN** a transaction's counterparty has no merchant entry
- **THEN** the transaction row SHALL show the raw counterparty name

#### Scenario: Linked name shows merchant display name

- **WHEN** a transaction's counterparty name is linked to a merchant with a display name
- **THEN** the transaction row SHALL show that display name instead of the raw counterparty name

### Requirement: Merchant admin page

The system SHALL provide a page listing every distinct counterparty name seen in any user's transactions, along with its mapping status (unmapped, mapped, no-logo, or linked to another merchant), transaction count, and first-seen date. The page SHALL default to showing unmapped names first and SHALL support searching by name and sorting by name, transaction count, first-seen date, and status through server-driven requests that re-render only the list.

#### Scenario: Page lists all names

- **WHEN** a user opens the merchant page
- **THEN** every distinct counterparty name from all transactions SHALL be listed with its status, transaction count, and first-seen date

#### Scenario: Linked name shows its target merchant

- **WHEN** a distinct counterparty name is linked to a merchant
- **THEN** the list SHALL show the name as linked to that merchant

#### Scenario: Unmapped names first by default

- **WHEN** a user opens the merchant page without changing sort
- **THEN** names without a merchant entry SHALL appear before mapped or explicitly no-logo names
- **THEN** within the unmapped names, those with more transactions SHALL appear before those with fewer

#### Scenario: Search filters the list

- **WHEN** a user enters a search term
- **THEN** the list SHALL only show names matching the term and SHALL be re-rendered via a server-driven request without a full page reload

#### Scenario: Sort changes ordering

- **WHEN** a user selects a sort column or direction
- **THEN** the list SHALL re-render via a server-driven request ordered by that column and direction without a full page reload

#### Scenario: Link picker shows candidate merchants

- **WHEN** a user opens the link picker for an unmapped name
- **THEN** existing canonical merchants SHALL be listed, most frequently used first, and the list SHALL narrow as the user types a name

#### Scenario: Link picker empty result offers creation

- **WHEN** a user's link-picker search matches no existing merchant
- **THEN** the picker SHALL offer to create a new merchant instead of a link

#### Scenario: Empty history

- **WHEN** a user opens the merchant page and no transactions exist
- **THEN** the page SHALL render without errors and SHALL show an empty list

### Requirement: Merchant mapping actions

The system SHALL allow a user to map a counterparty name to a new canonical merchant by setting its display name and logo URL, link a counterparty name to an existing merchant, mark a merchant as "no logo", and clear a name's mapping, each through a server-driven request. Actions SHALL persist to the global map and SHALL be reflected in subsequent overview renders.

#### Scenario: Map merchant with display name and logo

- **WHEN** a user maps a counterparty name with a display name and a logo URL
- **THEN** a merchant entry SHALL be stored with that display name and URL
- **THEN** the list SHALL update to show the name as mapped
- **THEN** the overview SHALL render that logo and display name for transactions with that name

#### Scenario: Update existing mapping

- **WHEN** a user maps a counterparty name that already has a merchant entry
- **THEN** the entry SHALL be updated with the new display name and logo URL

#### Scenario: Mark as no logo

- **WHEN** a user marks a counterparty name as "no logo"
- **THEN** a merchant entry SHALL be stored with the "no logo" decision
- **THEN** the list SHALL update to show the name as explicitly no-logo

#### Scenario: Link name to existing merchant

- **WHEN** a user links a counterparty name to an existing merchant
- **THEN** a link SHALL be stored from that name to the merchant
- **THEN** the list SHALL show the name as linked to that merchant
- **THEN** the overview SHALL render that merchant's logo and display name for transactions with that name

#### Scenario: Clear a linked name

- **WHEN** a user clears a counterparty name that is linked to a merchant
- **THEN** the link SHALL be removed
- **THEN** the name SHALL appear as unmapped again

#### Scenario: Clear a mapped merchant

- **WHEN** a user clears a canonical merchant that has linked names
- **THEN** the merchant entry and all links pointing to it SHALL be removed
- **THEN** those names SHALL appear as unmapped again

#### Scenario: Invalid logo URL

- **WHEN** a user submits a logo URL that is not a valid absolute HTTP or HTTPS URL
- **THEN** the action SHALL NOT create or modify a merchant entry
- **THEN** the list SHALL remain unchanged
