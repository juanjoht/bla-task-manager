using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Abstractions.Security;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Infrastructure.Persistence;

/// <summary>Creates the demo user and sample tasks through the domain, so seed data obeys the same rules as real data.</summary>
public sealed class DbSeeder(AppDbContext db, IPasswordHasher passwordHasher, IClock clock)
{
    public const string DemoEmail = "demo@ballastlane.test";
    public const string DemoPassword = "Demo123!";

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        // Idempotent: it runs on every API start, so it must do nothing once the demo user exists.
        if (await db.Users.AnyAsync(u => u.Email == DemoEmail, cancellationToken))
        {
            return;
        }

        var demo = new User(DemoEmail, "Demo User", passwordHasher.Hash(DemoPassword), clock.UtcNow);
        db.Users.Add(demo);
        db.Tasks.AddRange(SampleTasks(demo.Id));

        await db.SaveChangesAsync(cancellationToken);
    }

    private IEnumerable<TaskItem> SampleTasks(Guid userId)
    {
        yield return CreateTask(userId, "Prepare project presentation", "Slides, demo script and architecture diagram.", 1, TaskItemStatus.InProgress);
        yield return CreateTask(userId, "Review pull requests", "Two pending reviews from the team.", 2, TaskItemStatus.Pending);
        yield return CreateTask(userId, "Write unit tests for TaskService", "Cover ownership rules and validation.", 0, TaskItemStatus.Done);
        yield return CreateTask(userId, "Update README with setup steps", "Prerequisites, run commands and demo credentials.", 5, TaskItemStatus.Pending);
        yield return CreateTask(userId, "Plan next sprint", null, 7, TaskItemStatus.Pending);
    }

    private TaskItem CreateTask(Guid userId, string title, string? description, int dueInDays, TaskItemStatus status)
    {
        var dueDate = clock.Today.AddDays(dueInDays);
        var task = new TaskItem(userId, title, description, dueDate, clock.Today, clock.UtcNow);

        if (status != TaskItemStatus.Pending)
        {
            task.Update(title, description, status, dueDate, clock.UtcNow);
        }

        return task;
    }
}
