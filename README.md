# Task Manager – Ballast Lane technical exercise

A small task manager built with **.NET 10** (API) and **Angular 22** (UI), following **Clean Architecture** and **TDD**.

> **User story:** As a registered user, I want to log in and manage my tasks (create, view, edit, delete)
> so that I can organize my work. Each task has a title, description, status and due date, and belongs to the logged-in user.

## Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 22](https://nodejs.org/) (includes npm) for the Angular app

No database installation is needed: the API uses **SQLite** and creates its database file on first run.

## Quick start
Run the API and the web app in two terminals, from the repository root:

```bash
dotnet run --project src/TaskManager.Api --launch-profile http
```

```bash
cd frontend
npm install
npm start
```

Open **http://localhost:4200** and sign in with the demo account below.

> `npm install` may report that a few install scripts were blocked (`esbuild`, `lmdb`, `@parcel/watcher`,
> `msgpackr-extract`). They belong to Angular's build tooling and are not needed: build and tests work without them.

## Run the API
```bash
dotnet run --project src/TaskManager.Api --launch-profile http
```
On startup the API applies the EF Core migrations and seeds demo data (only the first time).

| What | URL |
|---|---|
| Swagger UI | http://localhost:5276/swagger |
| Health check (public) | http://localhost:5276/api/health |

### Demo credentials
| Email | Password |
|---|---|
| `demo@ballastlane.test` | `Demo123!` |

The demo user has 5 sample tasks in different statuses, with due dates relative to today.

### Try it in Swagger
1. `POST /api/auth/login` with the demo credentials and copy the `token` from the response.
2. Click **Authorize** and paste the token.
3. Call the `/api/tasks` endpoints.

To start again from a clean database, stop the API and delete `src/TaskManager.Api/taskmanager.db`.

## Run the tests
Backend:
```bash
dotnet test TaskManager.slnx
```
Unit tests for Domain and Application, SQLite-backed tests for Infrastructure, and in-memory integration tests
(`WebApplicationFactory`) that call the real HTTP endpoints.

Frontend (Vitest):
```bash
cd frontend
npm test -- --watch=false
```

## Web app (Angular)
| Page | Route | Access |
|---|---|---|
| Sign in (shows the demo credentials) | `/login` | Public |
| Create account | `/register` | Public |
| My tasks: status filter, overdue badge, edit, delete with a confirmation dialog | `/tasks` | Logged in |
| New task / Edit task | `/tasks/new`, `/tasks/:id/edit` | Logged in |

```
frontend/src/app/
  core/       AuthService (session in a signal + localStorage), auth interceptor (bearer token, logout on 401),
              auth guard, API base URL, app shell (top bar)
  features/   auth/ (login, register) and tasks/ (TaskService, list, form)
  shared/     models mirroring the API DTOs, form validators with the API's rules, error messages,
              date helpers, reusable confirm dialog
```
Standalone components, signals for state (no NgRx), reactive forms, lazy-loaded routes and Bootstrap 5 styles
(CSS only, no Bootstrap JavaScript). The UI is responsive down to phone width.

## API

| Area | Verb | Route | Auth | Responses |
|---|---|---|---|---|
| Auth | POST | `/api/auth/register` | Public | 201 · 400 · 409 |
| Auth | POST | `/api/auth/login` | Public | 200 + token · 400 · 401 |
| Auth | GET | `/api/auth/me` | Token | 200 · 401 |
| Health | GET | `/api/health` | Public | 200 |
| Tasks | GET | `/api/tasks?status=` | Token | 200 |
| Tasks | GET | `/api/tasks/{id}` | Token | 200 · 404 |
| Tasks | POST | `/api/tasks` | Token | 201 + Location · 400 |
| Tasks | PUT | `/api/tasks/{id}` | Token | 200 · 400 · 404 |
| Tasks | DELETE | `/api/tasks/{id}` | Token | 204 · 404 |

Errors use the standard `ProblemDetails` format. A user only ever sees their own tasks: another user's task returns 404.

## Architecture
```
src/
  TaskManager.Domain          Entities (TaskItem, User) that enforce their own rules. No dependencies.
  TaskManager.Application     Use cases (AuthService, TaskService), DTOs, FluentValidation validators,
                              and the interfaces it needs (repositories, hasher, token generator, clock).
  TaskManager.Infrastructure  Implementations: EF Core + SQLite, repositories, PBKDF2 password hasher,
                              JWT generator, business time zone clock, migrations and demo seeder.
  TaskManager.Api             Thin controllers, composition root, JWT auth, validation filter,
                              exception-to-ProblemDetails handler, Swagger UI, CORS.
tests/
  TaskManager.<Layer>.Tests   One test project per layer (xUnit, FluentAssertions, Moq).
```
Dependencies point inwards: `Api → Application/Infrastructure → Domain`. Domain and Application know nothing about
EF Core, SQLite, HTTP or JWT.

## Configuration (`src/TaskManager.Api/appsettings*.json`)
| Setting | Purpose |
|---|---|
| `ConnectionStrings:Default` | SQLite file (`taskmanager.db`) |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpirationMinutes` | Token settings |
| `Jwt:SigningKey` | Development key in `appsettings.Development.json` only; use a secret store in production |
| `App:TimeZone` | Business time zone used for "today" (`America/Bogota`) |
| `Cors:AllowedOrigins` | Browser origins allowed to call the API (the Angular app) |

## Documentation
- [`docs/PLAN.md`](docs/PLAN.md) – implementation plan and progress
- [`docs/genai.md`](docs/genai.md) – how GenAI (Claude Code) was used, validated and corrected
