# Worknexa

Worknexa — Your Work. One Intelligent Place.

This repository is the Worknexa client and API. The client opens on a signed-in workspace. Business modules (tasks, calendar, team, learning, and the rest) are not implemented yet.

## Layout

```
frontend/     React 19, TypeScript, Vite
backend/      ASP.NET Core clean architecture
database/     SQL scripts
docker/       SQL Server for local development
docs/         Architecture notes
```

## Frontend

From `frontend/`:

```bash
npm install
npm run dev
```

The app is served at http://localhost:5173/.

Other commands:

```bash
npm run lint
npm run build
npm run preview
```

`npm run build` typechecks with `tsc` and then builds the production bundle. There is no frontend test runner yet. Use lint and build as the frontend checks.

Copy `frontend/.env.example` to `frontend/.env` if you want to point the client at a different API:

```
VITE_API_URL=http://localhost:5080
```

The project board does not call the API. A health query hook exists for later use and stays disabled.

## Backend

From `backend/`:

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/API/Worknexa.Api.csproj
```

The API listens on http://localhost:5080/. `GET /api/health` returns `{ "status": "ok" }`. In Development, Swagger UI is at http://localhost:5080/swagger.

Projects:

| Project | Role |
|---|---|
| `src/Domain` | Domain model. |
| `src/Application` | Validation, paging, sorting, and the current-user abstraction. |
| `src/Infrastructure` | Entity Framework Core, SQL Server, and the database health check. |
| `src/API` | HTTP host, Swagger, Serilog, CORS, correlation ids, and error handling. |
| `tests/Worknexa.Api.Tests` | API tests. |

## Database

The API uses PostgreSQL. Tables live in the `worknexa` schema of database `worknexa_db`.

The API reads `appsettings.json`, then `appsettings.{Environment}.json`, then environment variables. Override the connection string with `ConnectionStrings__Worknexa` and the environment with `ASPNETCORE_ENVIRONMENT`.

The local development connection string is in `backend/src/API/appsettings.Development.json`. Other environments have no default and must set `ConnectionStrings__Worknexa`:

```
Host=localhost;Port=5432;Database=worknexa_db;Username=postgres;Password=<your-password>;Search Path=worknexa
```

Create the empty database before applying migrations. Entity Framework creates the `worknexa` schema, tables, and seed data when you apply migrations. The entity relationship diagram and table notes are in `docs/database/`.

## Migrations

Install the EF tool once from `backend/`:

```bash
dotnet tool install --global dotnet-ef
```

Add a migration when the model changes:

```bash
dotnet ef migrations add <Name> --project src/Infrastructure/Worknexa.Infrastructure.csproj --startup-project src/API/Worknexa.Api.csproj --output-dir Persistence/Migrations
```

Apply migrations:

```bash
dotnet ef database update --project src/Infrastructure/Worknexa.Infrastructure.csproj --startup-project src/API/Worknexa.Api.csproj
```

The initial migration creates the `worknexa` schema, tables, and seed data: roles, permissions, leave types, and the local sign-in accounts.

## Sign-in

The API uses a signed JWT. The browser stores it in an httpOnly cookie named `worknexa.session`. The client also holds the token in memory and sends it as `Authorization: Bearer`; it never writes the token to web storage, so a reload restores the session from the cookie. Cookie-authenticated writes must carry the `X-Worknexa-Request` header, which blocks cross-site form posts. Sign-in is limited to 10 attempts per minute per client address and password changes to 5 per minute per person (`RateLimits` in `appsettings.json`). Passwords are hashed with ASP.NET Core Identity's password hasher. Permission checks read the user's roles from the database, so a new role is a `Roles` row plus `RolePermissions` rows. Components ask a shared `can()` helper instead of naming permissions inline.

Development accounts, all using the local password `Worknexa_Dev123!`:

| Email | Role |
|---|---|
| `admin@worknexa.local` | Admin |
| `manager@worknexa.local` | Manager |
| `member@worknexa.local` | Team Member |

That password is only for this machine. Do not reuse it outside development.

In Development the API signs tokens with the key in `appsettings.Development.json`. Production must set `Auth__SigningKey` to a unique secret of at least 32 characters. The API refuses to start in Production with the development key.

Sign in at `/login`. The workspace routes require a session. Profile and password changes are at `/profile`. Sign out is in the account menu.

## Tests

Backend:

```bash
cd backend
dotnet test
```

Frontend:

```bash
cd frontend
npm run lint
npm run build
```
