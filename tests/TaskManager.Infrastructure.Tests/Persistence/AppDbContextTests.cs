using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Infrastructure.Tests.Common;

namespace TaskManager.Infrastructure.Tests.Persistence;

public sealed class AppDbContextTests : IDisposable
{
    private static readonly DateTime UtcNow = new(2026, 11, 11, 15, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = DateOnly.FromDateTime(UtcNow);

    private readonly SqliteTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private User SaveUser(string email = "demo@ballastlane.test")
    {
        var user = new User(email, "Demo User", "hashed-password", UtcNow);
        using var context = _database.CreateContext();
        context.Users.Add(user);
        context.SaveChanges();
        return user;
    }

    private TaskItem SaveTask(TaskItem task)
    {
        using var context = _database.CreateContext();
        context.Tasks.Add(task);
        context.SaveChanges();
        return task;
    }

    [Fact]
    public void User_IsSavedAndReadBackWithAllFields()
    {
        var user = SaveUser();

        using var context = _database.CreateContext();
        var loaded = context.Users.Single(u => u.Id == user.Id);

        loaded.Email.Should().Be("demo@ballastlane.test");
        loaded.Name.Should().Be("Demo User");
        loaded.PasswordHash.Should().Be("hashed-password");
        loaded.CreatedAt.Should().Be(UtcNow);
    }

    [Fact]
    public void Task_IsSavedAndReadBackWithAllFields()
    {
        var user = SaveUser();
        var task = new TaskItem(user.Id, "Prepare demo", "Rehearse", Today.AddDays(2), Today, UtcNow);
        task.Update("Prepare demo", "Rehearse", TaskItemStatus.InProgress, Today.AddDays(2), UtcNow);
        SaveTask(task);

        using var context = _database.CreateContext();
        var loaded = context.Tasks.Single(t => t.Id == task.Id);

        loaded.UserId.Should().Be(user.Id);
        loaded.Title.Should().Be("Prepare demo");
        loaded.Description.Should().Be("Rehearse");
        loaded.Status.Should().Be(TaskItemStatus.InProgress);
        loaded.DueDate.Should().Be(Today.AddDays(2));
    }

    [Fact]
    public void Email_MustBeUnique()
    {
        SaveUser("demo@ballastlane.test");

        var act = () => SaveUser("demo@ballastlane.test");

        act.Should().Throw<DbUpdateException>();
    }

    [Fact]
    public void Task_RequiresAnExistingUser()
    {
        var orphan = new TaskItem(Guid.NewGuid(), "Orphan task", null, Today, Today, UtcNow);

        var act = () => SaveTask(orphan);

        act.Should().Throw<DbUpdateException>();
    }

    [Fact]
    public void OverdueTask_CanStillBeLoaded()
    {
        // Created when its due date was valid; loading it later must not re-apply the "not in the past" rule.
        var user = SaveUser();
        var longAgo = new DateOnly(2020, 1, 1);
        var task = SaveTask(new TaskItem(user.Id, "Old task", null, longAgo, longAgo, UtcNow));

        using var context = _database.CreateContext();
        var loaded = context.Tasks.Single(t => t.Id == task.Id);

        loaded.DueDate.Should().Be(longAgo);
    }

    [Fact]
    public void Dates_AreReadBackAsUtc()
    {
        // SQLite has no time zone type; without a converter dates come back as "Unspecified"
        // and would be serialized without the trailing "Z", so the browser would treat them as local time.
        var user = SaveUser();
        var task = SaveTask(new TaskItem(user.Id, "Title", null, Today, Today, UtcNow));

        using var context = _database.CreateContext();

        context.Users.Single(u => u.Id == user.Id).CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
        var loadedTask = context.Tasks.Single(t => t.Id == task.Id);
        loadedTask.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
        loadedTask.UpdatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Status_IsStoredAsReadableText()
    {
        var user = SaveUser();
        var task = new TaskItem(user.Id, "Title", null, Today, Today, UtcNow);
        task.Update("Title", null, TaskItemStatus.Done, Today, UtcNow);
        SaveTask(task);

        _database.ExecuteScalar("SELECT Status FROM Tasks").Should().Be("Done");
    }
}
