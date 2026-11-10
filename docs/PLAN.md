# Implementation Plan – Task Manager (Ballast Lane exercise)

Deadline: presentation Friday Nov 13, 10:00 am (Bogotá). Read CLAUDE.md first for rules.

## Data model

**TaskItem**
| Field | Type | Rule |
|---|---|---|
| Id | Guid (PK) | System generated |
| Title | string | Required, max 100 chars |
| Description | string? | Optional, max 500 chars |
| Status | enum: Pending, InProgress, Done | Default Pending |
| DueDate | DateOnly | Cannot be in the past on creation |
| UserId | Guid (FK) | Owner, taken from the JWT |
| CreatedAt / UpdatedAt | DateTime (UTC) | Simple audit |

**User**
| Field | Type | Rule |
|---|---|---|
| Id | Guid (PK) | System generated |
| Email | string | Required, valid format, unique |
| PasswordHash | string | Never plain text |
| Name | string | Required |
| CreatedAt | DateTime (UTC) | Simple audit |

Seed: demo@ballastlane.test / Demo123! with 5 sample tasks in different statuses.

## Endpoints

| API | Verb | Route | Auth | Responses |
|---|---|---|---|---|
| Auth | POST | /api/auth/register | Public | 201 · 400 invalid · 409 duplicate email |
| Auth | POST | /api/auth/login | Public | 200 + token · 401 |
| Auth | GET | /api/auth/me | Token | 200 · 401 |
| Auth | GET | /api/health | Public | 200 (non-authorized endpoint example) |
| Tasks | GET | /api/tasks | Token | 200 list (optional status filter) |
| Tasks | GET | /api/tasks/{id} | Token | 200 · 404 if missing or not owned |
| Tasks | POST | /api/tasks | Token | 201 + Location · 400 |
| Tasks | PUT | /api/tasks/{id} | Token | 200 · 400 · 404 |
| Tasks | DELETE | /api/tasks/{id} | Token | 204 · 404 |

Cross-cutting: error middleware returning ProblemDetails, Swagger/OpenAPI with JWT support, CORS for the Angular app.

## Frontend (Angular)

- Screens: Login, Register, Task list (status filter, edit, delete with confirmation), Create/Edit task form.
- `core/`: AuthService, JWT interceptor, auth guard. `features/auth/`, `features/tasks/`: standalone components. `shared/`: models.
- State with services + signals (no NgRx). Reactive forms with the same rules as the backend.
- Responsive, user-visible error messages, no browser console warnings.

## Phases (one commit per step, tests first)

1. Solution structure + project references → `chore: solution structure`
2. Domain tests (red) → implementation (green)
3. Application: services, DTOs, FluentValidation, repository interfaces (tests first)
4. Infrastructure: DbContext (SQLite), migration, repositories, password hasher, JWT generator, seed data (tests first)
5. Api: AuthController, TasksController, JWT config, error middleware, Swagger, CORS + WebApplicationFactory tests
6. Angular frontend
7. README (setup, run, demo user, architecture) + docs/genai.md (prompt, sample output, how AI output was validated and corrected)
8. Clean clone test following the README

## Progress
- [x] Phase 1
- [ ] Phase 2
- [ ] Phase 3
- [ ] Phase 4
- [ ] Phase 5
- [ ] Phase 6
- [ ] Phase 7
- [ ] Phase 8
