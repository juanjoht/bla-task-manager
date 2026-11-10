# Project: Task Manager – Ballast Lane Technical Exercise

## User story
As a registered user, I want to log in and manage my tasks (create, view, edit, delete)
so that I can organize my work. Each task has a title, description, status and due date,
and belongs to the logged-in user.

## Plan
The full implementation plan (data model, endpoints, screens, phases, progress) is in docs/PLAN.md.
Read it at the start of every session and update the Progress checkboxes when a phase is done.

## Stack
- .NET 10 (LTS), C#, ASP.NET Core Web API (controllers)
- EF Core with SQLite (zero setup for reviewers), migrations + seed data
- JWT authentication, passwords hashed (never stored in plain text)
- Validation: domain invariants in entity constructors/methods; input validation with FluentValidation in Application
- Solution name: TaskManager (.NET 10 default solution format)
- Tests: xUnit + Moq (or NSubstitute) + FluentAssertions; WebApplicationFactory for API tests
- Frontend: Angular (latest stable), standalone components, responsive, no console warnings

## Architecture (Clean Architecture)
- `src/TaskManager.Domain` – entities, enums, domain rules. No dependencies.
- `src/TaskManager.Application` – use cases/services, DTOs, validation, repository interfaces. Depends only on Domain.
- `src/TaskManager.Infrastructure` – EF Core DbContext, repositories, password hashing, JWT token generation. Implements Application interfaces.
- `src/TaskManager.Api` – controllers, DI setup, auth config, error handling middleware. No business logic here.
- `tests/` – one test project per layer (Domain, Application, Infrastructure, Api).
- `frontend/` – Angular app.

## Rules
- Work in small steps. One layer or feature at a time; stop and wait for review after each step.
- TDD: write the failing tests first, show them, then write the minimum code to pass, then refactor.
- Business rules and validation live in Domain/Application only, never in controllers or repositories.
- Use correct HTTP verbs and status codes (200, 201 + Location, 204, 400, 401, 404).
- A user can only see and modify their own tasks.
- Keep code readable: small methods, clear names, no dead code, nullable reference types enabled.
- Do not add packages or features that were not requested without asking first.
- Seed a demo user (demo@ballastlane.test / Demo123!) and a few sample tasks.
- Keep README.md updated with setup and run instructions.
