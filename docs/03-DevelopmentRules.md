\# 03 - Development Rules



\## Dependency Rules

\- Domain depends only on SharedKernel

\- Application depends on Domain + SharedKernel

\- Infrastructure depends on Application contracts + Domain

\- Web and API depend on Application/Contracts through proper boundaries

\- AdminApp only calls API through HTTP client contracts, never DB



\## Business Logic Placement

Allowed:

\- Domain

\- Application



Not allowed:

\- Controller

\- Razor PageModel

\- View

\- WPF ViewModel

\- EF entity configuration

\- Program.cs



\## Auth Rules

\- Web: Cookie auth

\- API/WPF: JWT + Refresh Token

\- Permission-based authorization required for sensitive actions



\## Migration Rules

\- Only Infrastructure owns DbContext and migrations

\- One migration per meaningful change

\- Migration name must be descriptive

\- Never mix unrelated schema changes



\## Audit Rules

\- Create/Update/Delete and sensitive overrides must be audited

\- Audit log should capture actor, action, target, timestamp, summary



\## Async Rules

\- Async end-to-end

\- No .Result / .Wait()

\- No manual thread management unless explicitly justified



\## Definition of Done

A task is done when:

\- build passes

\- migration is created if needed

\- API/Web/WPF impact is considered

\- authorization is checked

\- audit is checked

\- tests exist at least at minimum level

\- changed files are documented

