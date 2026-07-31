# test-mode-auth Specification

## Purpose

Lets Playwright tests authenticate without going through the real OIDC flow by providing a test user identifier in an HTTP header.

## Requirements

### Requirement: Test user authentication via header

The system SHALL authenticate as a test user when the `X-Test-User` header is present on a request, bypassing the normal OIDC authentication flow entirely.

#### Scenario: Request with valid X-Test-User header

- **WHEN** an HTTP request arrives with header `X-Test-User: <guid>`
- **THEN** the system SHALL find or create a User with username matching the GUID value
- **THEN** the system SHALL authenticate the request as that user
- **THEN** the system SHALL NOT redirect to OIDC login
- **THEN** the system SHALL serve the requested page normally

#### Scenario: Request without X-Test-User header

- **WHEN** an HTTP request arrives without the `X-Test-User` header
- **THEN** the system SHALL use the normal OIDC authentication flow
- **THEN** no test-mode code SHALL run

#### Scenario: User does not exist

- **WHEN** an HTTP request arrives with `X-Test-User: <new-guid>`
- **THEN** the system SHALL create a new User with username set to the GUID value
- **THEN** the system SHALL authenticate as the newly created user

#### Scenario: Multiple concurrent test users

- **WHEN** two requests arrive with different `X-Test-User` GUID values
- **THEN** each request SHALL be authenticated as its own user
- **THEN** each user's data SHALL be fully isolated from the other

#### Scenario: Test user data isolation

- **WHEN** a test user creates transactions via CSV upload
- **THEN** those transactions SHALL only be visible when querying as that test user
- **THEN** other test users SHALL NOT see those transactions in their budget view

### Requirement: Session persistence for test users

The system SHALL maintain a session for test-authenticated users so that subsequent requests with the same header within the same session do not re-authenticate on every request.

#### Scenario: Test user maintains session

- **WHEN** a test user is authenticated via `X-Test-User`
- **THEN** the system SHALL create a session cookie for that user
- **THEN** subsequent requests with the session cookie SHALL remain authenticated as that test user

### Requirement: No side effects on production

The test mode authentication mechanism SHALL be completely inactive when the `X-Test-User` header is absent, with zero performance impact on production requests.

#### Scenario: Production request unaffected

- **WHEN** a request arrives without `X-Test-User` header
- **THEN** the middleware SHALL pass through without any additional processing
- **THEN** the request SHALL proceed through the normal authentication middleware chain

### Requirement: Test mode indicator

The system SHALL make the test mode status available to the rest of the application so that downstream layers can optionally behave differently in test mode.

#### Scenario: Request context marks test mode

- **WHEN** a request is authenticated via `X-Test-User`
- **THEN** `request.test_mode` SHALL be `True`
- **WHEN** a request goes through normal OIDC auth
- **THEN** `request.test_mode` SHALL be `False` (or absent)
