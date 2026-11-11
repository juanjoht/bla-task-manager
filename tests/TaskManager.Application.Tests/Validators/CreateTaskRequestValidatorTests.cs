using FluentValidation.TestHelper;
using TaskManager.Application.Dtos.Tasks;
using TaskManager.Application.Tests.Common;
using TaskManager.Application.Validators;

namespace TaskManager.Application.Tests.Validators;

public class CreateTaskRequestValidatorTests
{
    private static readonly DateTime UtcNow = new(2026, 11, 11, 15, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = DateOnly.FromDateTime(UtcNow);
    private static readonly CreateTaskRequest ValidRequest = new("Prepare demo", "Rehearse", Today.AddDays(1));

    private readonly CreateTaskRequestValidator _validator = new(new FixedClock(UtcNow, Today));

    [Fact]
    public void ValidRequest_HasNoErrors()
    {
        _validator.TestValidate(ValidRequest).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingTitle_IsRejected(string title)
    {
        _validator.TestValidate(ValidRequest with { Title = title })
            .ShouldHaveValidationErrorFor(x => x.Title).WithErrorMessage("Title is required.");
    }

    [Fact]
    public void TitleOverMaxLength_IsRejected()
    {
        _validator.TestValidate(ValidRequest with { Title = new string('a', 101) })
            .ShouldHaveValidationErrorFor(x => x.Title).WithErrorMessage("Title cannot exceed 100 characters.");
    }

    [Fact]
    public void NullDescription_IsAllowed()
    {
        _validator.TestValidate(ValidRequest with { Description = null }).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void DescriptionOverMaxLength_IsRejected()
    {
        _validator.TestValidate(ValidRequest with { Description = new string('a', 501) })
            .ShouldHaveValidationErrorFor(x => x.Description).WithErrorMessage("Description cannot exceed 500 characters.");
    }

    [Fact]
    public void DueDateInThePast_IsRejected()
    {
        _validator.TestValidate(ValidRequest with { DueDate = Today.AddDays(-1) })
            .ShouldHaveValidationErrorFor(x => x.DueDate).WithErrorMessage("Due date cannot be in the past.");
    }

    [Fact]
    public void DueDateToday_UsesTheBusinessDate()
    {
        // 21:00 in Bogotá = 02:00 next day in UTC: "today" must come from the clock's business date.
        var utcNow = new DateTime(2026, 11, 12, 2, 0, 0, DateTimeKind.Utc);
        var businessToday = new DateOnly(2026, 11, 11);
        var validator = new CreateTaskRequestValidator(new FixedClock(utcNow, businessToday));

        validator.TestValidate(ValidRequest with { DueDate = businessToday }).ShouldNotHaveAnyValidationErrors();
    }
}
