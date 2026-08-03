# Loremetry

Angular + ASP.NET Core monorepo for the Loremetry book-analysis workspace.

## Stack

- **Frontend:** Angular 22, signals, Tailwind CSS 4, Clerk (`ngx-clerk`)
- **Backend:** ASP.NET Core 10 minimal API, EF Core + PostgreSQL
- **Database:** PostgreSQL 17 (Docker Compose)

## Prerequisites

- Node.js 22+ and npm
- .NET 10 SDK
- Docker (for Postgres)

## Quick start

1. Copy environment template and fill in values:

   ```bash
   cp .env.example .env
   ```

2. Start Postgres:

   ```bash
   docker compose up -d
   ```

3. Start the backend (from `backend/`):

   ```bash
   dotnet run
   ```

   API: `http://localhost:5092`

4. Start the frontend (from `frontend/`):

   ```bash
   npm install
   npm start
   ```

   App: `http://localhost:4200` (proxies `/api` to the backend)

Or use the **Full Stack + Postgres** launch configuration in VS Code / Cursor.

## Environment variables

See [`.env.example`](.env.example). Key variables:

| Variable | Purpose |
|----------|---------|
| `CLERK_PUBLISHABLE_KEY` | Clerk publishable key for the Angular app |
| `CLERK_JWT_ISSUER` | Clerk Frontend API URL (issuer), without `/.well-known/...` |
| `OPERATOR_KEY` | Break-glass operator token for admin access |
| `anthropic_api_key`, `tokenmix_api_key`, etc. | Provider credentials (seeded to DB on startup) |

`.env` is gitignored. On deployment, set the same names in your host environment.

## Auth

- **Clerk users** sign in via the login page when `CLERK_JWT_ISSUER` is set.
- **Operator access** uses `OPERATOR_KEY` via the break-glass form on the login page.
- Admin routes and `/api/admin/*` endpoints require operator (break-glass) access.

### Clerk session token

In the Clerk dashboard, customize the session token to include email:

```json
{ "email": "{{user.primary_email_address}}" }
```

## Project layout

```
backend/          ASP.NET Core API
frontend/         Angular SPA
scripts/dev/      Postgres and backend wait helpers
docker-compose.yml
```

## Health checks

- `GET /health` — API liveness
- `GET /health/db` — Postgres connectivity

## Production notes

- Set `apiBaseUrl` in `frontend/src/environments/environment.ts` if the API is not same-origin.
- Configure CORS in `backend/Program.cs` for your production frontend origin.
- Do not expose admin SQL console endpoints without operator auth (already enforced).
