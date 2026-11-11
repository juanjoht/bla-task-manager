using FluentAssertions;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Exceptions;

namespace TaskManager.Domain.Tests.Entities;

public class TaskItemTests
{
    private static readonly DateTime UtcNow = new(2026, 11, 11, 15, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = DateOnly.FromDateTime(UtcNow);
    private static readonly Guid UserId = Guid.NewGuid();

    private static TaskItem CreateValidTask() =>
        TaskItem.Create(UserId, "Prepare demo", "Rehearse the presentation", Today.AddDays(2), UtcNow);

    // ---------- Create ----------

    [Fact]
    public void Create_WithValidData_SetsAllProperties()
    {
        var dueDate = Today.AddDays(3);

        var task = TaskItem.Create(UserId, "Prepare demo", "Rehearse the presentation", dueDate, UtcNow);

        task.Id.Should().NotBeEmpty();
        task.UserId.Should().Be(UserId);
        task.Title.Should().Be("Prepare demo");
        task.Description.Should().Be("Rehearse the presentation");
        task.DueDate.Should().Be(dueDate);
        task.CreatedAt.Should().Be(UtcNow);
        task.UpdatedAt.Should().Be(UtcNow);
    }

    [Fact]
    public void Create_NewTask_StartsAsPending()
    {
        var task = CreateValidTask();

        task.Status.Should().Be(TaskItemStatus.Pending);
    }

    [Fact]
    public void Create_TwoTasks_GetDifferentIds()
    {
        var first = CreateValidTask();
        var second = CreateValidTask();

        first.Id.Should().NotBe(second.Id);
    }

    [Fact]
    public void Create_WithEmptyUserId_Throws()
    {
        var act = () => TaskItem.Create(Guid.Empty, "Title", null, Today, UtcNow);

        act.Should().Throw<DomainException>().WithMessage("User id is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingTitle_Throws(string? title)
    {
        var act = () => TaskItem.Create(UserId, title!, null, Today, UtcNow);

        act.Should().Throw<DomainException>().WithMessage("Title is required.");
    }

    [Fact]
    public void Create_TrimsTitle()
    {
        var task = TaskItem.Create(UserId, "  Prepare demo  ", null, Today, UtcNow);

        task.Title.Should().Be("Prepare demo");
    }

    [Fact]
    public void Create_WithTitleAtMaxLength_Succeeds()
    {
        var title = new string('a', TaskItem.TitleMaxLength);

        var task = TaskItem.Create(UserId, title, null, Today, UtcNow);

        task.Title.Should().HaveLength(TaskItem.TitleMaxLength);
    }

    [Fact]
    public void Create_WithTitleOverMaxLength_Throws()
    {
        var title = new string('a', TaskItem.TitleMaxLength + 1);

        var act = () => TaskItem.Create(UserId, title, null, Today, UtcNow);

        act.Should().Throw<DomainException>().WithMessage("Title cannot exceed 100 characters.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankDescription_StoresNull(string? description)
    {
        var task = TaskItem.Create(UserId, "Title", description, Today, UtcNow);

        task.Description.Should().BeNull();
    }

    [Fact]
    public void Create_WithDescriptionAtMaxLength_Succeeds()
    {
        var description = new string('a', TaskItem.DescriptionMaxLength);

        var task = TaskItem.Create(UserId, "Title", description, Today, UtcNow);

        task.Description.Should().HaveLength(TaskItem.DescriptionMaxLength);
    }

    [Fact]
    public void Create_WithDescriptionOverMaxLength_Throws()
    {
        var description = new string('a', TaskItem.DescriptionMaxLength + 1);

        var act = () => TaskItem.Create(UserId, "Title", description, Today, UtcNow);

        act.Should().Throw<DomainException>().WithMessage("Description cannot exceed 500 characters.");
    }

    [Fact]
    public void Create_WithDueDateToday_Succeeds()
    {
        var task = TaskItem.Create(UserId, "Title", null, Today, UtcNow);

        task.DueDate.Should().Be(Today);
    }

    [Fact]
    public void Create_WithDueDateInThePast_Throws()
    {
        var act = () => TaskItem.Create(UserId, "Title", null, Today.AddDays(-1), UtcNow);

        act.Should().Throw<DomainException>().WithMessage("Due date cannot be in the past.");
    }

    // ---------- Update ----------

    [Fact]
    public void Update_WithValidData_ChangesFieldsAndUpdatedAt()
    {
        var task = CreateValidTask();
        var later = UtcNow.AddHours(1);
        var newDueDate = Today.AddDays(10);

        task.Update("New title", "New description", TaskItemStatus.InProgress, newDueDate, later);

        task.Title.Should().Be("New title");
        task.Description.Should().Be("New description");
        task.Status.Should().Be(TaskItemStatus.InProgress);
        task.DueDate.Should().Be(newDueDate);
        task.UpdatedAt.Should().Be(later);
    }

    [Fact]
    public void Update_DoesNotChangeIdentityOwnerOrCreatedAt()
    {
        var task = CreateValidTask();
        var originalId = task.Id;

        task.Update("New title", null, TaskItemStatus.Done, Today, UtcNow.AddHours(1));

        task.Id.Should().Be(originalId);
        task.UserId.Should().Be(UserId);
        task.CreatedAt.Should().Be(UtcNow);
    }

    [Fact]
    public void Update_WithPastDueDate_IsAllowed()
    {
        // An overdue task must stay editable (e.g. to mark it Done) without forcing a new date.
        var task = CreateValidTask();
        var pastDate = Today.AddDays(-5);

        task.Update("Title", null, TaskItemStatus.Done, pastDate, UtcNow.AddDays(1));

        task.DueDate.Should().Be(pastDate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithMissingTitle_Throws(string? title)
    {
        var task = CreateValidTask();

        var act = () => task.Update(title!, null, TaskItemStatus.Pending, Today, UtcNow);

        act.Should().Throw<DomainException>().WithMessage("Title is required.");
    }

    [Fact]
    public void Update_WithDescriptionOverMaxLength_Throws()
    {
        var task = CreateValidTask();
        var description = new string('a', TaskItem.DescriptionMaxLength + 1);

        var act = () => task.Update("Title", description, TaskItemStatus.Pending, Today, UtcNow);

        act.Should().Throw<DomainException>().WithMessage("Description cannot exceed 500 characters.");
    }

    [Fact]
    public void Update_WithUndefinedStatus_Throws()
    {
        var task = CreateValidTask();

        var act = () => task.Update("Title", null, (TaskItemStatus)99, Today, UtcNow);

        act.Should().Throw<DomainException>().WithMessage("Status is not valid.");
    }

    [Fact]
    public void Update_WhenInvalid_LeavesTaskUnchanged()
    {
        var task = CreateValidTask();

        var act = () => task.Update("Changed", null, (TaskItemStatus)99, Today, UtcNow.AddHours(1));

        act.Should().Throw<DomainException>();
        task.Title.Should().Be("Prepare demo");
        task.Description.Should().Be("Rehearse the presentation");
        task.Status.Should().Be(TaskItemStatus.Pending);
        task.UpdatedAt.Should().Be(UtcNow);
    }
}
