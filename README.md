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

1. Edit [`.env`](.env) in the repo root and fill in your values (Clerk, operator key, provider keys).

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

See [`.env`](.env). Key variables:

| Variable | Purpose |
|----------|---------|
| `CLERK_PUBLISHABLE_KEY` | Clerk publishable key for the Angular app |
| `CLERK_JWT_ISSUER` | Clerk Frontend API URL (issuer), without `/.well-known/...` |
| `OPERATOR_KEY` | Break-glass operator token for admin access |
| `anthropic_api_key`, `tokenmix_api_key`, etc. | Provider credentials (seeded to DB on startup) |
| `default_provider` | AI provider for chat (`tokenmix` or `anthropic`; assistant uses TokenMix today) |
| `default_model` | TokenMix model id for writing assistant chat (optional; backend falls back to `gpt-4o-mini`) |

`.env` is gitignored. On deployment (Miget), set the **same variable names** in your host app environment.

### TokenMix / AI (local testing)

1. Add your key to `.env`:

   ```bash
   tokenmix_api_key=your-key-here
   default_provider=tokenmix
   default_model=gpt-4o-mini   # optional
   ```

2. Restart the backend so values seed into `lore.platform_settings` (or paste the key in **Admin → Platform**).

3. Verify: sign in as operator → **Admin → Platform** → **Test all AI & API**. TokenMix should show **Connected** (`GET /v1/models`).

4. Use the Write panel assistant chat — requests go through `POST /api/writing/assistant/chat`; the API key never reaches the browser.

On Miget, set `tokenmix_api_key`, `default_provider`, and optionally `default_model` as app environment variables (no `.env` file in the container).

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
