# bla-task-manager
This is a technical exercise interview to Ballast lane Applications

## Prerequisites
- .NET 10 SDK

## Build and test
```bash
dotnet build TaskManager.slnx
dotnet test TaskManager.slnx
```

## Solution structure
```
src/
  TaskManager.Domain          entities, enums, domain rules (no dependencies)
  TaskManager.Application     use cases, DTOs, validation, repository interfaces (→ Domain)
  TaskManager.Infrastructure  EF Core (SQLite), repositories, hashing, JWT (→ Application)
  TaskManager.Api             controllers, DI, auth, error handling (→ Application, Infrastructure)
tests/
  TaskManager.<Layer>.Tests   one xUnit project per layer
```
