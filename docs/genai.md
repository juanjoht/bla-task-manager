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
- Entities have private setters and factory methods (`Create`) so an invalid entity can never exist.
- Time is passed in (`utcNow`) instead of reading `DateTime.UtcNow` inside the domain, which makes tests deterministic.
- "Due date cannot be in the past" applies **only on creation**: an overdue task must remain editable (e.g. to mark it Done).
- `Update` validates everything before mutating, so a failed update leaves the entity unchanged (covered by a test).
- Email is trimmed and lower-cased in the domain so the uniqueness check in later phases is case-insensitive.
- Max lengths are public constants reused by validators and EF configuration (single source of truth).

**Open point carried to Phase 3:** "today" is computed from UTC. For a user in Bogotá (UTC-5), after 19:00 local time
the UTC date is already tomorrow, so a due date of "today" would be rejected. To be decided in the Application layer.
