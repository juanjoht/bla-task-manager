# GenAI usage log

Tool: **Claude Code** (desktop app), used as a pair programmer under explicit rules.
This file is updated at the end of every phase and becomes the "Generative AI tools" section of the exercise.

## How the AI was steered

1. **Analysis first, code later.** Before writing any code, the exercise PDF was analysed with Claude to produce
   two guardrail files that the agent reads at the start of every session:
   - [`CLAUDE.md`](../CLAUDE.md): stack, Clean Architecture boundaries and non-negotiable rules
     (TDD, no business logic in controllers, correct status codes, no unrequested packages, stop after each step).
   - [`docs/PLAN.md`](PLAN.md): data model, endpoints, screens and phased delivery with a progress checklist.
2. **Small, reviewable steps.** Every session prompt asks for one phase only, and the agent must stop for human review.
3. **Kickoff prompt used:**
   > Read CLAUDE.md and docs/PLAN.md. Summarize the plan in 5 lines so I can confirm you understood it.
   > Then start Phase 1 and stop after it so I can review.

   Asking for a summary first is a cheap check that the agent understood the plan before it changes anything.

## Phase log

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
