# Budget

## Software Architecture Decisions

### 1. Project Structure

- **Decision:** Default Django structure (with the main app directory called `config`)
- **Rationale:** Easy to pick up later. No need to use anything other than the defaults Django have chosen.
- **Alternatives Considered:** Two Scoops of Django structure, as I've been long time fan.
However, would require me to keep a bible to know the structure.
- **Impact:** Improves maintainability

### 2. Database Design

- **Decision:** PostgreSQL with Django ORM
- **Rationale:** [Why this database was chosen]
- **Alternatives Considered:** [e.g., MySQL, SQLite]
- **Impact:** [How this affects performance or scalability]

### 3. Authentication & Authorization

- **Decision:** [e.g., Django’s built-in auth system with JWT for API]
- **Rationale:** [Why this approach was chosen]
- **Alternatives Considered:** [e.g., OAuth2, custom auth]
- **Impact:** [How this affects security or user management]

### 4. API Design

- **Decision:** [e.g., RESTful API with Django REST Framework]
- **Rationale:** [Why this framework was chosen]
- **Alternatives Considered:** [e.g., GraphQL, FastAPI]
- **Impact:** [How this affects development speed or API consistency]

### 5. Testing Strategy

- **Decision:** Unit tests with `unittest`
- **Rationale:** Django's suggested way of unit testing. Plus it's the Python built in (same reasoning as using MsTest in .NET projects).
- **Alternatives Considered:** Pytest. Again, I'm no full time Python dev.
- **Impact:** Improves maintainability.

### 6. Deployment

- **Decision:** [e.g., Docker containers with CI/CD via GitHub Actions]
- **Rationale:** [Why this deployment method was chosen]
- **Alternatives Considered:** [e.g., Manual deployment, Heroku]
- **Impact:** [How this affects deployment reliability or speed]

### 7. Performance Optimization

- **Decision:** [e.g., Caching with Redis, database indexing]
- **Rationale:** [Why this optimization was chosen]
- **Alternatives Considered:** [e.g., No caching, Memcached]
- **Impact:** [How this affects user experience or server load]

### 8. Monitoring & Logging

- **Decision:** [e.g., Sentry for error tracking, Django logging for application logs]
- **Rationale:** [Why this monitoring approach was chosen]
- **Alternatives Considered:** [e.g., Custom logging, Datadog]
- **Impact:** [How this affects issue resolution or debugging]

### 9. Frontend stack

- **Decision:** Use HTMX and AlpineJS for main frontend development.
- **Rationale:** I did not want to rely on a obese frontend framework this time, still thinking that small packages like HTMX and AlpineJS are more than enough to get usability/flashiness off the floor.
Also, I want to use as much Python as possible.
- **Alternatives Considered:** None really. The whole point of this project was to use as much Python as possible.
- **Impact:** Should make developing a breeze.

- **Decision:** Use PicoCSS as main css library with Bootstrap grid library for the layouting.
- **Rationale:** Use as little styling as possible but still get a nice looking app.
Some customization is still required, but PicoCSS really has nice css variables that you can tweak.
- **Alternatives Considered:** [BeerCSS](https://github.com/beercss/beercss)
- **Impact:** Should make developing a breeze.

- **Decision:** Use [Lucide](https://www.lucide.dev) for icons.
- **Rationale:** Works very nicely with CDN and inplace rendering of icons.
- **Alternatives Considered:** Bootstrap Icons. Didn't use it this time because of been-there-done-that mentality.
- **Impact:** Should make developing a breeze.