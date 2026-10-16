# WorkNexa Architecture & Development Guardrails

## 1. Architectural Integrity & Boundaries
- **Integration Boundary**: React client components must NEVER import third-party service SDKs (e.g., Google Calendar, Outlook, Jira, GitHub, Slack, OpenAI). All external integrations must flow through backend connector ports.
- **Backend Clean Architecture**:
  - `Worknexa.Domain`: Plain C# entities, value objects, and domain events. No dependencies on outer layers.
  - `Worknexa.Application`: Business logic, CQRS handlers, FluentValidation validators, abstractions (`ICurrentUser`).
  - `Worknexa.Infrastructure`: EF Core DbContext, PostgreSQL mappings (`worknexa` schema), repository implementations.
  - `Worknexa.Api`: HTTP endpoints, Swagger, Auth handlers, Rate limiting, and Serilog logging.
  - Dependencies must strictly point inward: API -> Infrastructure/Application -> Domain.

## 2. Security & Authentication Guardrails
- **Session & Token Storage**: The client holds JWT access tokens exclusively in memory (`setAccessToken`). Tokens MUST NEVER be written to `localStorage` or `sessionStorage`.
- **Session Cookie**: Authentication sessions rely on the `worknexa.session` HttpOnly cookie.
- **CSRF Defense**: All API requests from the frontend client must send the `X-Worknexa-Request: 1` header.
- **Authorization**: Access checks must use the shared `can(granted, permission)` helper and explicit permissions from `access.ts`. Avoid hardcoding permission strings inline.
- **Backend Passwords & Keys**: Never commit production keys. Validate that `Auth__SigningKey` meets the 32+ character requirement in production environments.

## 3. Frontend Standards & Conventions
- **TypeScript Strictness**: `tsconfig.app.json` has `erasableSyntaxOnly` and `verbatimModuleSyntax` enabled.
  - Always use `import type` for type-only imports.
  - Use TypeScript string unions instead of runtime `enum`s.
- **Styling & Tokens**: Maintain CSS variables defined in `src/styles/tokens.css` and `src/styles/worknexa.css`. Tailwind CSS utilities are enabled, but preflight is disabled to preserve core design system resets. Do NOT introduce ad-hoc color schemes.
- **Components & Layout**: Keep page features inside `WorkspaceShell` (`AppShell`). Reuse primitives from `src/components/ui` (`OutlineButton`, `IconButton`, `Dropdown`, `PriorityBadge`, `CountBadge`, `AvatarGroup`).

## 4. Database & Migrations
- **Target Database**: PostgreSQL with schema `worknexa` in database `worknexa_db`.
- **Migration Command Standard**:
  ```bash
  dotnet ef migrations add <Name> \
    --project src/Infrastructure/Worknexa.Infrastructure.csproj \
    --startup-project src/API/Worknexa.Api.csproj \
    --output-dir Persistence/Migrations
  ```

## 5. Verification & Testing Standards
- When modifying backend logic, verify with `dotnet test` from `backend/`.
- When modifying frontend code, verify type checking and test suites with:
  ```bash
  npm run lint
  npm run test
  npm run build
  ```
