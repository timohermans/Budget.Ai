## MODIFIED Requirements

### Requirement: Test user authentication via header

The system SHALL authenticate as a test user when the `X-Test-User` header is present on a request, bypassing the normal OIDC authentication flow entirely. The header value SHALL be used directly as the user id - no user record is created or looked up.

#### Scenario: Request with valid X-Test-User header

- **WHEN** an HTTP request arrives with header `X-Test-User: <id>`
- **THEN** the system SHALL authenticate the request as a user whose id is the header value
- **THEN** the system SHALL NOT redirect to OIDC login
- **THEN** the system SHALL serve the requested page normally

#### Scenario: Request without X-Test-User header

- **WHEN** an HTTP request arrives without the `X-Test-User` header
- **THEN** the system SHALL use the normal OIDC authentication flow
- **THEN** no test-mode code SHALL run

#### Scenario: User does not exist

- **WHEN** an HTTP request arrives with `X-Test-User: <new-id>` and no transactions exist for that id
- **THEN** the system SHALL authenticate the request as a user with that id
- **THEN** the user's overview SHALL render as empty

#### Scenario: Multiple concurrent test users

- **WHEN** two requests arrive with different `X-Test-User` values
- **THEN** each request SHALL be authenticated as its own user
- **THEN** each user's data SHALL be fully isolated from the other

#### Scenario: Test user data isolation

- **WHEN** a test user creates transactions via CSV upload
- **THEN** those transactions SHALL only be visible when querying as that test user
- **THEN** other test users SHALL NOT see those transactions in their budget view

## ADDED Requirements

### Requirement: Test mode bypasses antiforgery validation

The system SHALL NOT require antiforgery tokens on requests authenticated in test mode.

#### Scenario: Test mode POST without token

- **WHEN** a test-authenticated request performs a POST without an antiforgery token
- **THEN** the request SHALL NOT be rejected for missing the token

#### Scenario: Production POST without token

- **WHEN** a non-test-mode request performs a POST without an antiforgery token
- **THEN** the request SHALL be rejected for missing the token
