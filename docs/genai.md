# Generative AI tools

**Tool:** Claude Code (desktop app), used as a pair programmer: the AI proposed and wrote code, the human set the
rules, reviewed every step and approved every commit. This document answers the exercise's GenAI section:
the prompt (1), a representative sample of the output (2), and how the output was validated (3), corrected (4)
and made robust for edge cases, authentication and validation (5). The appendix logs every phase in detail.

---

## 1. The prompt

### 1.1 Prompt to generate the task-management REST API
This is the prompt I would use to generate the API in one go. It condenses what this project taught me about
steering the model: give it context and hard constraints, list the edge cases explicitly, define "done" as tests,
and make it stop for review instead of generating everything at once.

```text
You are a senior .NET engineer. Build a RESTful task-management API in C# / ASP.NET Core (.NET 10, controllers)
following Clean Architecture and TDD.

## Context
- Users manage only their own tasks. A basic User model exists (Id, Email, Name, PasswordHash, CreatedAt).
- Task: Id (Guid), Title, Description, Status, DueDate, UserId (owner), CreatedAt, UpdatedAt.

## Architecture (dependencies point inwards)
- Domain: entities that enforce their own invariants (private setters, validating constructor). No dependencies.
- Application: use-case services, DTOs (records), FluentValidation validators, and the interfaces it needs
  (ITaskRepository, IUserRepository, IPasswordHasher, ITokenGenerator, IClock). Depends only on Domain.
- Infrastructure: EF Core + SQLite, repositories, password hasher, JWT generator, clock. Implements Application interfaces.
- Api: thin controllers (no business logic), composition root, JWT bearer auth, global validation filter,
  exception handler that returns ProblemDetails.

## Business rules
- Title: required (not blank), max 100. Description: optional, max 500, blank stored as null.
- Status: Pending | InProgress | Done. New tasks always start as Pending.
- DueDate is a calendar date (DateOnly). It cannot be in the past when CREATING (compare with "today" in the
  configured business time zone, not UTC). Editing an overdue task must be allowed.
- A user can only read/update/delete their own tasks. A task that does not exist OR belongs to someone else
  returns 404 (never 403, so task ids cannot be discovered). This check lives in the Application service, not in SQL.

## Endpoints
- POST /api/auth/register (201 + Location, 400, 409 duplicate email), POST /api/auth/login (200 + JWT, 401),
  GET /api/auth/me (token), GET /api/health (public).
- GET /api/tasks?status=, GET /api/tasks/{id}, POST /api/tasks (201 + Location), PUT /api/tasks/{id} (200),
  DELETE /api/tasks/{id} (204). All task endpoints require a token; the user id comes from the token's "sub" claim.
- Errors: ProblemDetails. 400 returns every validation error at once. Unknown exceptions: generic 500, no internals.

## Security
- Passwords hashed with a salted, slow algorithm (use ASP.NET Core Identity's hasher; no custom crypto).
- Same 401 message for unknown email and wrong password. Emails are case-insensitive and unique.
- The JWT contains only sub, email and name; signing key from configuration, at least 32 characters, validated at startup.

## Quality bar
- Write the failing tests first (xUnit + FluentAssertions + Moq), show them, then the minimum code to pass.
- Unit tests for Domain and Application; repository tests against real SQLite in memory (not the EF InMemory
  provider); integration tests with WebApplicationFactory, including "user B cannot see/update/delete user A's task".
- Nullable enabled, 0 build warnings, no dead code, no packages beyond the ones named here without asking.

## Way of working
- Before coding, summarize your understanding in 5 lines and list any assumption you had to make.
- Deliver one layer at a time (Domain → Application → Infrastructure → Api) and stop after each for review.
```

### 1.2 How the AI was actually steered in this project
The prompt above is the distilled version. In practice the work was driven in three layers:

1. **Guardrail files, written before any code** by analysing the exercise PDF with Claude:
   [`CLAUDE.md`](../CLAUDE.md) (stack, architecture boundaries, non-negotiable rules) and
   [`docs/PLAN.md`](PLAN.md) (data model, endpoints, screens, phases). The agent reads both at the start of every session,
   so the rules do not have to be repeated in each prompt.
2. **Kickoff prompt:**
   > Read CLAUDE.md and docs/PLAN.md. Summarize the plan in 5 lines so I can confirm you understood it.
   > Then start Phase 1 and stop after it so I can review.

   The summary is a cheap check that the model understood the plan before it changes anything.
3. **Steering prompts during the work**, which changed how the agent behaved for the rest of the project:
   - *"Can we do this step by step so I can see what you are creating?"* → one small red → green step at a time.
   - *"Please ask me before every commit."* → files, message and test results shown before each commit.
   - *"Commits must not be under your name."* → no AI attribution in the history.
   - Design questions that changed the output: *"why do the entities have so much logic?"* (refactor to a simpler
     `Guard` style), *"can we split DTOs and services into folders?"*, *"the delete looks poor, can we use a dialog?"*.

---

## 2. Representative output
All of the code in this repository was generated through this process. A few representative pieces:

**Domain entity that cannot be invalid** – [`TaskItem.cs`](../src/TaskManager.Domain/Entities/TaskItem.cs)
```csharp
public TaskItem(Guid userId, string title, string? description, DateOnly dueDate, DateOnly today, DateTime utcNow)
{
    Guard.NotEmpty(userId, "User id");
    Guard.NotInPast(dueDate, today, "Due date");

    Id = Guid.NewGuid();
    UserId = userId;
    Title = Guard.RequiredMaxLength(title, TitleMaxLength, "Title");
    Description = Guard.OptionalMaxLength(description, DescriptionMaxLength, "Description");
    Status = TaskItemStatus.Pending;
    DueDate = dueDate;
    CreatedAt = utcNow;
    UpdatedAt = utcNow;
}
```

**Ownership as a business rule** – [`TaskService.cs`](../src/TaskManager.Application/Services/TaskService.cs)
```csharp
// Same 404 for "does not exist" and "belongs to another user", so other users' task ids cannot be discovered.
private async Task<TaskItem> GetOwnedTaskAsync(Guid userId, Guid taskId, CancellationToken cancellationToken)
{
    var task = await tasks.GetByIdAsync(taskId, cancellationToken);
    if (task is null || task.UserId != userId)
    {
        throw new NotFoundException("Task not found.");
    }

    return task;
}
```

**Thin controller** – [`TasksController.cs`](../src/TaskManager.Api/Controllers/TasksController.cs)
```csharp
[HttpPost]
public async Task<ActionResult<TaskDto>> Create(CreateTaskRequest request, CancellationToken cancellationToken)
{
    var task = await taskService.CreateAsync(User.GetUserId(), request, cancellationToken);
    return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
}
```

**End-to-end ownership test** – [`TasksControllerTests.cs`](../tests/TaskManager.Api.Tests/Controllers/TasksControllerTests.cs)
```csharp
(await intruder.GetFromJsonAsync<List<TaskDto>>("/api/tasks", ApiJson.Options)).Should().BeEmpty();
(await intruder.GetAsync($"/api/tasks/{demoTask.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
(await intruder.DeleteAsync($"/api/tasks/{demoTask.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
```

**Size of the result:** 171 backend tests (Domain 48, Application 56, Infrastructure 38, Api 29) and 62 Angular tests,
0 build warnings, delivered in 57 small commits, including 22 red (failing tests) → green (implementation) pairs
where the red commit always comes first.

---

## 3. How the AI's suggestions were validated
1. **Tests before code, and reading the red.** Every step started with failing tests. The failure messages were read,
   not just counted: some tests passed "by accident" before the code existed (e.g. a 404 because the route did not exist
   yet) and were called out; others were kept as protective tests that must stay green after the change.
2. **Human review of every step and every commit.** Each step was shown (files, test output, explanation) and committed
   only after an explicit "yes". Several design changes came from these reviews.
3. **Zero warnings as a signal.** Build warnings were treated as findings (e.g. a vulnerable transitive package).
4. **Running the real thing, not only the tests.** After each green step: the API with Swagger and curl, the Angular app in
   a browser against the real API, the browser's console and network log, and a phone-width viewport.
   Several issues that all tests passed were only visible this way.
5. **Checking dependencies.** Package versions were matched to the .NET 10 LTS target, licenses checked
   (FluentAssertions 8 is commercial → 7.2.2), and blocked npm install scripts inspected before deciding.

## 4. Corrections and improvements to the AI output (highlights)
| # | What the AI produced or claimed | Problem | Correction |
|---|---|---|---|
| 1 | Due date rule compared with `DateTime.UtcNow` | In Bogotá (UTC-5) after 19:00 a task due "today" was rejected | Domain receives the business "today"; `IClock` computes it from a configured time zone; a test reproduces 21:00 in Bogotá |
| 2 | Settings read while registering services | Integration tests could not override them (no JWT key) | Options pattern with lazy binding and `ValidateOnStart` |
| 3 | Swagger marked every endpoint as secured | Wrong docs for public endpoints (tests passed) | Operation transformer adds security only to `[Authorize]` endpoints |
| 4 | Dates read back from SQLite | `Kind = Unspecified` → serialized without "Z" → shifted 5 h in the browser | Model-wide converter reads every `DateTime` as UTC (caught by a test) |
| 5 | `DatePipe` for `"2026-11-14"` | Parsed as midnight UTC → shown as Nov 13 in Bogotá | Dates formatted from the local calendar |
| 6 | Angular `Validators.email` / `required` | Accepted `user@domain` and blank titles that the API rejects | Same regex as the API and a `notBlank` validator |
| 7 | `== PasswordVerificationResult.Success` | Would reject valid passwords whose hash uses older parameters | `!= Failed` |
| 8 | Latest EF Core / Identity packages (11.0.0) | Target .NET 11, not the .NET 10 LTS of the project | Pinned 10.0.15 |
| 9 | "Real SQLite enforces max lengths" | False (TEXT has no limit) | Corrected explicitly; lengths enforced by domain and validators |
| 10 | Expired token kept in localStorage | 401 in the console and a flash of the list when coming back | Expired sessions dropped on startup (UX only; the API still validates) |
| 11 | Rich entities with static factories | Correct but harder to explain | Human-driven refactor to constructors + `Guard` (assertions untouched, all tests green) |
| 12 | Plain CSS proposed for the UI | Slower and less consistent given the deadline | Re-evaluated after the human asked; Bootstrap (CSS only) |

## 5. Edge cases, authentication and validation
- **Authentication:** JWT bearer with issuer, audience, signature and lifetime validated on every request; signing key
  length validated at startup; passwords hashed with PBKDF2 + salt (ASP.NET Core Identity); the user id comes only from
  the token; the frontend sends the token only to the API origin and logs out on 401.
- **Authorization / ownership:** enforced in the Application service and proven by unit tests and an end-to-end HTTP test.
  Foreign and missing tasks both return 404.
- **No account enumeration:** same login error for unknown email and wrong password; emails normalized (trim + lower case)
  and unique (validated in the service and enforced by a unique index).
- **Validation in depth:** Angular forms (immediate feedback) → FluentValidation (all errors at once, 400) → Domain
  invariants (last line of defence) → database constraints. Same limits and messages on every layer.
- **Edge cases covered by tests:** blank and whitespace-only values, maximum lengths ±1, invalid enum values, past due dates
  on create vs update, overdue tasks still editable and loadable, atomic updates (a failed update leaves the entity
  unchanged), time zone boundaries, duplicate emails with different casing, invalid/expired tokens, idempotent seeding,
  migrations in sync with the model.

---

## Appendix – phase-by-phase log

### Phase 1 – Solution structure
**Generated:** solution (`.slnx`), 4 layer projects, 4 test projects, project references pointing inwards.

**Validated / corrected:**
| Finding | Action |
|---|---|
| `dotnet new webapi` template pulled `Microsoft.OpenApi 2.0.0`, flagged NU1903 (high-severity advisory). | Bumped `Microsoft.AspNetCore.OpenApi` to the patched 10.0.15 instead of suppressing the warning. Build is now warning-free. |
| Template sample code (`WeatherForecast`, `Class1.cs`, `UnitTest1.cs`). | Removed: no dead code. |
| FluentAssertions v8+ changed to a commercial license. | Chose FluentAssertions **7.2.2** (Apache 2.0), pinned to an exact version. |
| Dependency direction. | Verified by reading the `.csproj` files: Domain has no references, Application → Domain only. |

### Phase 2 – Domain (TDD)
**Process:** tests written first against stubs throwing `NotImplementedException`, committed as **red**
(47 failing), then the minimum implementation, committed as **green** (47 passing). Both commits are in the history.

**Design decisions reviewed by the human:**
- Status enum named `TaskItemStatus`, not `TaskStatus`: avoids a clash with `System.Threading.Tasks.TaskStatus`,
  which is imported by implicit usings.
- Entities have private setters and validating constructors, so an invalid entity can never exist.
- **Refactor step (human-driven):** the first green version used static `Create(...)` factories with inline checks.
  After review it was judged harder to explain than necessary, so it was refactored to plain constructors plus a small
  `Guard` helper (`Guard.RequiredMaxLength(title, TitleMaxLength, "Title")`), making each entity read as a list of rules.
  Assertions were untouched (only the call syntax changed) and all 47 tests stayed green.
- EF Core loads rows through a private parameterless constructor, so creation-only rules (due date not in the past)
  are not re-applied to existing overdue tasks.
- A fully anemic model (rules only in Application services) was considered and rejected: it is simpler to explain,
  but it leaves the Domain layer with no behaviour and allows `task.Title = ""` from anywhere.
- Time is passed in (`utcNow`) instead of reading `DateTime.UtcNow` inside the domain, which makes tests deterministic.
- "Due date cannot be in the past" applies **only on creation**: an overdue task must remain editable (e.g. to mark it Done).
- `Update` validates everything before mutating, so a failed update leaves the entity unchanged (covered by a test).
- Email is trimmed and lower-cased in the domain so the uniqueness check in later phases is case-insensitive.
- Max lengths are public constants reused by validators and EF configuration (single source of truth).

**Open point carried to Phase 3:** "today" is computed from UTC. For a user in Bogotá (UTC-5), after 19:00 local time
the UTC date is already tomorrow, so a due date of "today" would be rejected. Resolved in Phase 3 (see below).

### Phase 3 – Application (TDD, step by step)
**Process change:** after Phase 2 the human asked to work in much smaller steps and to approve **every commit**.
Each step now ends with: files shown → tests run (red or green) → explanation → explicit "yes" → commit.
This produced a red → green pair per component, visible in the history.

| Step | Commits |
|---|---|
| Contracts | `feat: add persistence and security abstractions` |
| AuthService | red (11 failing) → green |
| Time zone bug | red (test reproducing 21:00 Bogotá) → `fix` (green) → `refactor: introduce IClock` |
| Folder layout | `refactor: organize DTOs and services in type folders` (human request) |
| TaskService | red (15 failing) → green |
| Validators | red (22 failing, 8 "accept" cases passing) → green |

**Design decisions reviewed by the human:**
- Application defines interfaces (`IUserRepository`, `ITaskRepository`, `IPasswordHasher`, `ITokenGenerator`, `IClock`);
  Infrastructure will implement them (dependency inversion).
- **Ownership is a business rule, so it lives in `TaskService`, not in the repository:** `GetByIdAsync(id)` returns any task and
  `GetOwnedTaskAsync` throws `NotFoundException` when it is missing *or* owned by another user. Same 404 in both cases, so
  task ids of other users cannot be discovered. Covered by tests for get, update and delete.
- Login returns the same "Invalid email or password." for unknown email and wrong password (no account enumeration).
- Register checks the email before hashing, because hashing is deliberately slow.
- DTOs are `sealed record`s (immutable, value equality), one per file; `TaskDto` and `UserDto` never expose `UserId` or `PasswordHash`.
  Separate `CreateTaskRequest` (no status: new tasks are always Pending) and `UpdateTaskRequest`.
- Email normalization and format live once in the domain (`User.NormalizeEmail`, `User.IsValidEmail`) and are reused by
  services and validators. Max lengths come from entity constants. Shared title/description rules in `TaskRules`.
- Validators complement the domain: they return *all* errors at once for the UI and cover rules the domain cannot see
  (password strength on register only; login checks presence only, to avoid giving hints to an attacker).

**AI output corrected during review:**
| Finding | Action |
|---|---|
| The time zone issue flagged in Phase 2. | Human chose a configured business time zone. A domain test reproducing 21:00 in Bogotá was written first (red), then the fix (green). |
| The agent had first said a custom clock was unnecessary (`TimeProvider` is built in). | Revisited once "business today" was needed: `IClock` exposes `UtcNow` (audit) and `Today` (business date). The agent stated the change of position explicitly. |
| Email normalization would have been duplicated in `AuthService`. | Exposed `User.NormalizeEmail` / `User.IsValidEmail` as a single source of truth. |
| A validator using `GreaterThanOrEqualTo(clock.Today)` would freeze "today" when the validator is built. | Used `Must(date => date >= clock.Today)` so it is evaluated on every request. |
| `git rm` of the last file in a folder also removed the folder, so creating the replacement file failed. | Caught immediately by the build; folder recreated. Reported to the human rather than hidden. |

**Open points carried to later phases:**
- Phase 4: `IClock` implementation reading the time zone from configuration (with a test for the Bogotá case).
- Phase 5: run the validators automatically before controllers (400 with all errors) and map `ConflictException` → 409,
  `UnauthorizedException` → 401, `NotFoundException` → 404, `DomainException` → 400 in the error middleware.

### Phase 4 – Infrastructure (TDD, step by step)
Every component followed red → green with a human-approved commit for each step.

| Step | Contract implemented | Notes |
|---|---|---|
| `SystemClock` | `IClock` | `TimeProvider` + configured business time zone; test for 21:00 in Bogotá |
| `PasswordHasher` | `IPasswordHasher` | Wraps ASP.NET Core Identity's PBKDF2 hasher (random salt); no home-made crypto |
| `JwtTokenGenerator` | `ITokenGenerator` | HMAC-SHA256; `sub`, `email`, `name` claims only; signature tested with right and wrong keys |
| `AppDbContext` + configurations | – | Unique email index, FK task → user (cascade), status stored as text, ids generated by the domain |
| `UserRepository`, `TaskRepository` | repositories | Thin EF Core queries; list filtered by user and status, ordered by due date |
| `InitialCreate` migration | – | Test fails if the model changes without a new migration |
| `DbSeeder` | – | Demo user + 5 tasks created **through the domain**, dates relative to today, idempotent |

**Testing approach:** repositories and mapping are tested against a **real SQLite database in memory**, not EF Core's
InMemory provider, so constraints such as the unique email index and the foreign key are really enforced.

**Decisions taken by the human:**
- Simplicity first: a second time zone test (Tokyo) was removed to keep the clock easy to explain.
- SQLite kept for zero setup; switching to SQL Server would only touch Infrastructure (provider, connection string,
  new migration). Decision deferred until the application is complete.
- Demo data via a runtime seeder (domain rules, relative dates, testable) instead of `HasData` in the migration
  (fixed dates that become overdue, precomputed password hash).

**AI output corrected during review:**
| Finding | Action |
|---|---|
| The latest EF Core / Identity packages are 11.0.0, which target .NET 11. | Pinned 10.0.15 to match the .NET 10 LTS target. |
| The agent claimed real SQLite enforces max lengths. It does not (TEXT has no length limit). | Corrected explicitly; lengths stay enforced by the domain and validators, and `HasMaxLength` documents the schema for other providers. |
| SQLite returns `DateTime` values as `Unspecified`, which the API would serialize without "Z" (shifted 5 h in the browser). | Caught by a test; a model-wide converter reads every `DateTime` back as UTC. |
| `== PasswordVerificationResult.Success` would reject valid passwords whose hash uses older parameters. | Used `!= Failed`, so `SuccessRehashNeeded` also logs in. |
| A migration test opened a SQLite connection that was never disposed. | Fixed before committing (test class owns and disposes the connection). |
| `.gitignore` from the template did not ignore `*.db` files. | Added `*.db`, `*.db-shm`, `*.db-wal` in a separate commit. |
| A helper method named `Task(...)` read like the `Task` type. | Renamed to `CreateTask`. |

**Open points carried to Phase 5:**
- Register services (DI), bind `JwtOptions` and the time zone from appsettings, apply migrations and run `DbSeeder` on startup.
- Run validators automatically and map application exceptions to HTTP status codes (400, 401, 404, 409).

### Phase 5 – API (TDD with integration tests)
Integration tests use `WebApplicationFactory`: the real `Program`, middleware and controllers, with a per-test
in-memory SQLite database. Each step was a red → green pair approved by the human.

| Step | What |
|---|---|
| Startup | Composition root, settings, migrations + seeder on startup, public `GET /api/health` |
| Auth | Register / login / `me`, JWT bearer validation, validation filter, exception → ProblemDetails handler |
| Tasks | CRUD with 201 + Location, 204, 404; end-to-end test where a second user cannot see, edit or delete the demo user's task |
| Docs & CORS | OpenAPI + Swagger UI with an Authorize button; CORS restricted to the Angular origin |

**Design decisions reviewed by the human:**
- All dependency registration lives in the API (composition root), so inner layers need no DI/configuration packages.
- Controllers contain no logic: validation runs in a global filter, errors are translated by one exception handler,
  and the user id always comes from the token's `sub` claim.
- Unknown exceptions return a generic 500 without internal details.
- Status is serialized as text (`"Done"`) for readability and to avoid breaking clients if the enum is reordered.
- HTTPS redirection removed so reviewers can run on plain http without dev certificates.

**AI output corrected during review:**
| Finding | Action |
|---|---|
| Settings were read while registering services; `WebApplicationFactory` adds its configuration later, so the tests saw no JWT key. | Switched to the options pattern (lazy binding) with `ValidateOnStart`, keeping fail-fast on a missing/short signing key. |
| The first Swagger setup marked every endpoint as secured, including register, login and health. Tests passed; it was spotted by looking at the UI. | Operation transformer adds the security requirement only to `[Authorize]` endpoints. |
| One red test (update unknown task → 404) passed before the route existed. | Called out explicitly; it passes for the right reason once the controller exists. |
| A shell heredoc failed while writing a test file (quoting). | Nothing was written; files created with the editor tool instead. |

**Manual end-to-end check:** fresh database → API start → demo login → `GET /api/tasks` (5 tasks, status as text,
UTC dates) → 401 without token → Swagger UI shows the lock only on protected endpoints.

### Phase 6 – Angular frontend (TDD with Vitest)
Same red → green rhythm with a human-approved commit per step. After each green step the UI was checked in a
real browser against the running API (desktop and phone width, console open).

| Step | What |
|---|---|
| Scaffold | Angular 22 (standalone, Vitest, no SSR) + Bootstrap 5 CSS |
| Core | `AuthService` (session in a signal + localStorage), auth interceptor, auth guard |
| Auth pages | Login and register with the API's validation rules and messages |
| Tasks | `TaskService`, guarded app shell, task list (filter, overdue badge, empty states) |
| Task form | One page for create and edit |
| Delete dialog | Reusable accessible `ConfirmDialog` (human request: the inline confirmation looked poor) |
| Expired session | Stored sessions with an expired token are dropped on startup |

**Design decisions reviewed by the human:**
- **Bootstrap (CSS only)** instead of hand-written CSS: the agent first proposed plain CSS (zero dependencies); after the
  human asked, it re-weighed against the deadline and recommended Bootstrap for a consistent responsive UI. Bootstrap's
  JavaScript is not used; Angular controls all interaction (the modal is Bootstrap markup shown with `@if`).
- State with services + signals (no NgRx); `inject()` for dependencies; reactive forms; lazy-loaded routes.
- Token in `localStorage` for simplicity (trade-off vs an httpOnly cookie documented for the presentation).
- The interceptor sends the token **only** to the API origin and logs out on 401, except for login/register
  (there a 401 means "wrong password", not "expired token").
- Frontend validation mirrors the API exactly (same email regex, password rules, limits and messages); the API still
  validates everything, since the client cannot be trusted.

**AI output corrected during review:**
| Finding | Action |
|---|---|
| Angular's `Validators.email` accepts `user@domain`, which the API rejects. | Used the API's own regex so both sides agree. |
| `Validators.required` accepts a title made only of spaces; the API rejects it. | Added a `notBlank` validator. |
| `DatePipe` on `"2026-11-14"` reads it as midnight UTC → shown as Nov 13 in Bogotá. | Due dates are parsed and formatted from the local calendar (`shared/dates.ts`). |
| Coming back after the 60-minute token lifetime produced a `GET /api/tasks` → 401 in the console and a flash of the list. Found by reading the browser's network log. | `AuthService` drops stored sessions whose JWT `exp` has passed (UX only; the API still validates every token). |
| Long shell heredocs truncated two files while writing Angular code. | Caught before running tests; all code is now written with the editor tool. |
| Several red tests passed before the code existed (e.g. "never send the token to other hosts"). | Kept and called out as protective tests that must stay green after the implementation. |

**Browser console:** no warnings from the app. The only red line browsers print is the network log of an intentional
failed login (401), which every browser shows for 4xx responses.
