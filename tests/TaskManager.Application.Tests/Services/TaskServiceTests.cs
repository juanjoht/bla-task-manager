using FluentAssertions;
using Moq;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Dtos.Tasks;
using TaskManager.Application.Services;
using TaskManager.Application.Tests.Common;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Exceptions;

namespace TaskManager.Application.Tests.Services;

public class TaskServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 11, 11, 15, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = DateOnly.FromDateTime(UtcNow);
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();

    private readonly Mock<ITaskRepository> _tasks = new();

    private TaskService CreateService(DateTime? utcNow = null, DateOnly? today = null) =>
        new(_tasks.Object, new FixedClock(utcNow ?? UtcNow, today ?? Today));

    private static TaskItem TaskOwnedBy(Guid userId) =>
        new(userId, "Prepare demo", "Rehearse the presentation", Today.AddDays(2), Today, UtcNow);

    private void GivenStoredTask(TaskItem task) =>
        _tasks.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

    // ---------- List ----------

    [Fact]
    public async Task List_ReturnsTheCurrentUsersTasksAsDtos()
    {
        var task = TaskOwnedBy(OwnerId);
        _tasks.Setup(r => r.ListByUserAsync(OwnerId, null, It.IsAny<CancellationToken>())).ReturnsAsync([task]);

        var result = await CreateService().ListAsync(OwnerId, null, CancellationToken.None);

        result.Should().ContainSingle().Which.Should().Be(new TaskDto(
            task.Id, "Prepare demo", "Rehearse the presentation", TaskItemStatus.Pending, Today.AddDays(2), UtcNow, UtcNow));
    }

    [Fact]
    public async Task List_PassesTheStatusFilterToTheRepository()
    {
        _tasks.Setup(r => r.ListByUserAsync(OwnerId, TaskItemStatus.Done, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await CreateService().ListAsync(OwnerId, TaskItemStatus.Done, CancellationToken.None);

        result.Should().BeEmpty();
        _tasks.Verify(r => r.ListByUserAsync(OwnerId, TaskItemStatus.Done, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Get ----------

    [Fact]
    public async Task Get_OwnTask_ReturnsIt()
    {
        var task = TaskOwnedBy(OwnerId);
        GivenStoredTask(task);

        var result = await CreateService().GetByIdAsync(OwnerId, task.Id, CancellationToken.None);

        result.Id.Should().Be(task.Id);
    }

    [Fact]
    public async Task Get_MissingTask_ThrowsNotFound()
    {
        var act = () => CreateService().GetByIdAsync(OwnerId, Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Task not found.");
    }

    [Fact]
    public async Task Get_TaskOfAnotherUser_ThrowsNotFound()
    {
        // 404 (not 403) so nobody can discover which task ids exist.
        var task = TaskOwnedBy(OtherUserId);
        GivenStoredTask(task);

        var act = () => CreateService().GetByIdAsync(OwnerId, task.Id, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Task not found.");
    }

    // ---------- Create ----------

    [Fact]
    public async Task Create_SavesAPendingTaskOwnedByTheCurrentUser()
    {
        var request = new CreateTaskRequest("Prepare demo", "Rehearse", Today.AddDays(1));

        await CreateService().CreateAsync(OwnerId, request, CancellationToken.None);

        _tasks.Verify(r => r.AddAsync(
            It.Is<TaskItem>(t => t.UserId == OwnerId
                                 && t.Title == "Prepare demo"
                                 && t.Description == "Rehearse"
                                 && t.Status == TaskItemStatus.Pending
                                 && t.DueDate == Today.AddDays(1)
                                 && t.CreatedAt == UtcNow),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_ReturnsTheCreatedTask()
    {
        var request = new CreateTaskRequest("Prepare demo", null, Today);

        var result = await CreateService().CreateAsync(OwnerId, request, CancellationToken.None);

        result.Id.Should().NotBeEmpty();
        result.Title.Should().Be("Prepare demo");
        result.Status.Should().Be(TaskItemStatus.Pending);
    }

    [Fact]
    public async Task Create_UsesTheBusinessDateFromTheClock()
    {
        // 21:00 in Bogotá = 02:00 next day in UTC. A task due "today" (business date) must be accepted.
        var utcNow = new DateTime(2026, 11, 12, 2, 0, 0, DateTimeKind.Utc);
        var businessToday = new DateOnly(2026, 11, 11);
        var request = new CreateTaskRequest("Prepare demo", null, businessToday);

        var result = await CreateService(utcNow, businessToday).CreateAsync(OwnerId, request, CancellationToken.None);

        result.DueDate.Should().Be(businessToday);
    }

    [Fact]
    public async Task Create_WithPastDueDate_ThrowsAndDoesNotSave()
    {
        var request = new CreateTaskRequest("Prepare demo", null, Today.AddDays(-1));

        var act = () => CreateService().CreateAsync(OwnerId, request, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("Due date cannot be in the past.");
        _tasks.Verify(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- Update ----------

    [Fact]
    public async Task Update_OwnTask_AppliesChangesAndSaves()
    {
        var task = TaskOwnedBy(OwnerId);
        GivenStoredTask(task);
        var later = UtcNow.AddHours(2);
        var request = new UpdateTaskRequest("New title", null, TaskItemStatus.Done, Today.AddDays(5));

        var result = await CreateService(utcNow: later).UpdateAsync(OwnerId, task.Id, request, CancellationToken.None);

        result.Title.Should().Be("New title");
        result.Status.Should().Be(TaskItemStatus.Done);
        result.UpdatedAt.Should().Be(later);
        _tasks.Verify(r => r.UpdateAsync(task, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_MissingTask_ThrowsNotFound()
    {
        var request = new UpdateTaskRequest("Title", null, TaskItemStatus.Done, Today);

        var act = () => CreateService().UpdateAsync(OwnerId, Guid.NewGuid(), request, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Update_TaskOfAnotherUser_ThrowsNotFoundAndDoesNotChangeIt()
    {
        var task = TaskOwnedBy(OtherUserId);
        GivenStoredTask(task);
        var request = new UpdateTaskRequest("Hacked", null, TaskItemStatus.Done, Today);

        var act = () => CreateService().UpdateAsync(OwnerId, task.Id, request, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        task.Title.Should().Be("Prepare demo");
        _tasks.Verify(r => r.UpdateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- Delete ----------

    [Fact]
    public async Task Delete_OwnTask_DeletesIt()
    {
        var task = TaskOwnedBy(OwnerId);
        GivenStoredTask(task);

        await CreateService().DeleteAsync(OwnerId, task.Id, CancellationToken.None);

        _tasks.Verify(r => r.DeleteAsync(task, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_MissingTask_ThrowsNotFound()
    {
        var act = () => CreateService().DeleteAsync(OwnerId, Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Delete_TaskOfAnotherUser_ThrowsNotFoundAndDoesNotDelete()
    {
        var task = TaskOwnedBy(OtherUserId);
        GivenStoredTask(task);

        var act = () => CreateService().DeleteAsync(OwnerId, task.Id, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _tasks.Verify(r => r.DeleteAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
