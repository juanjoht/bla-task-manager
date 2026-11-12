using FluentAssertions;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Infrastructure.Persistence.Repositories;
using TaskManager.Infrastructure.Tests.Common;

namespace TaskManager.Infrastructure.Tests.Persistence;

public sealed class TaskRepositoryTests : IDisposable
{
    private static readonly DateTime UtcNow = new(2026, 11, 11, 15, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = DateOnly.FromDateTime(UtcNow);

    private readonly SqliteTestDatabase _database = new();
    private readonly User _owner;
    private readonly User _otherUser;

    public TaskRepositoryTests()
    {
        _owner = new User("owner@ballastlane.test", "Owner", "hash", UtcNow);
        _otherUser = new User("other@ballastlane.test", "Other", "hash", UtcNow);
        using var context = _database.CreateContext();
        context.Users.AddRange(_owner, _otherUser);
        context.SaveChanges();
    }

    public void Dispose() => _database.Dispose();

    private async Task<TaskItem> GivenSavedTask(User user, string title, DateOnly dueDate, TaskItemStatus status = TaskItemStatus.Pending)
    {
        var task = new TaskItem(user.Id, title, null, dueDate, Today, UtcNow);
        task.Update(title, null, status, dueDate, UtcNow);
        await using var context = _database.CreateContext();
        await new TaskRepository(context).AddAsync(task, CancellationToken.None);
        return task;
    }

    [Fact]
    public async Task Add_PersistsTheTask()
    {
        var task = await GivenSavedTask(_owner, "Prepare demo", Today);

        await using var context = _database.CreateContext();
        context.Tasks.Should().ContainSingle(t => t.Id == task.Id);
    }

    [Fact]
    public async Task ListByUser_ReturnsOnlyThatUsersTasks()
    {
        await GivenSavedTask(_owner, "Mine", Today);
        await GivenSavedTask(_otherUser, "Not mine", Today);
        await using var context = _database.CreateContext();

        var result = await new TaskRepository(context).ListByUserAsync(_owner.Id, null, CancellationToken.None);

        result.Should().ContainSingle().Which.Title.Should().Be("Mine");
    }

    [Fact]
    public async Task ListByUser_WithStatus_ReturnsOnlyMatchingTasks()
    {
        await GivenSavedTask(_owner, "Pending one", Today, TaskItemStatus.Pending);
        await GivenSavedTask(_owner, "Done one", Today, TaskItemStatus.Done);
        await using var context = _database.CreateContext();

        var result = await new TaskRepository(context).ListByUserAsync(_owner.Id, TaskItemStatus.Done, CancellationToken.None);

        result.Should().ContainSingle().Which.Title.Should().Be("Done one");
    }

    [Fact]
    public async Task ListByUser_OrdersByDueDate()
    {
        await GivenSavedTask(_owner, "Later", Today.AddDays(5));
        await GivenSavedTask(_owner, "Sooner", Today.AddDays(1));
        await using var context = _database.CreateContext();

        var result = await new TaskRepository(context).ListByUserAsync(_owner.Id, null, CancellationToken.None);

        result.Select(t => t.Title).Should().ContainInOrder("Sooner", "Later");
    }

    [Fact]
    public async Task GetById_ReturnsTheTaskOrNull()
    {
        var task = await GivenSavedTask(_owner, "Prepare demo", Today);
        await using var context = _database.CreateContext();
        var repository = new TaskRepository(context);

        (await repository.GetByIdAsync(task.Id, CancellationToken.None))!.Title.Should().Be("Prepare demo");
        (await repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task Update_PersistsTheChanges()
    {
        var saved = await GivenSavedTask(_owner, "Old title", Today);

        await using (var context = _database.CreateContext())
        {
            var repository = new TaskRepository(context);
            var task = (await repository.GetByIdAsync(saved.Id, CancellationToken.None))!;
            task.Update("New title", "Now with description", TaskItemStatus.Done, Today.AddDays(3), UtcNow.AddHours(1));
            await repository.UpdateAsync(task, CancellationToken.None);
        }

        await using var verify = _database.CreateContext();
        var reloaded = verify.Tasks.Single(t => t.Id == saved.Id);
        reloaded.Title.Should().Be("New title");
        reloaded.Description.Should().Be("Now with description");
        reloaded.Status.Should().Be(TaskItemStatus.Done);
        reloaded.DueDate.Should().Be(Today.AddDays(3));
    }

    [Fact]
    public async Task Delete_RemovesTheTask()
    {
        var saved = await GivenSavedTask(_owner, "To delete", Today);

        await using (var context = _database.CreateContext())
        {
            var repository = new TaskRepository(context);
            var task = (await repository.GetByIdAsync(saved.Id, CancellationToken.None))!;
            await repository.DeleteAsync(task, CancellationToken.None);
        }

        await using var verify = _database.CreateContext();
        verify.Tasks.Should().NotContain(t => t.Id == saved.Id);
    }
}
