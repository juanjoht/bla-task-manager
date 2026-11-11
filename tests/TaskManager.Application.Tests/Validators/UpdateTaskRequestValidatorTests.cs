using FluentValidation.TestHelper;
using TaskManager.Application.Dtos.Tasks;
using TaskManager.Application.Validators;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Tests.Validators;

public class UpdateTaskRequestValidatorTests
{
    private static readonly UpdateTaskRequest ValidRequest =
        new("Prepare demo", "Rehearse", TaskItemStatus.InProgress, new DateOnly(2026, 11, 20));

    private readonly UpdateTaskRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_HasNoErrors()
    {
        _validator.TestValidate(ValidRequest).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void MissingTitle_IsRejected()
    {
        _validator.TestValidate(ValidRequest with { Title = "" })
            .ShouldHaveValidationErrorFor(x => x.Title).WithErrorMessage("Title is required.");
    }

    [Fact]
    public void DescriptionOverMaxLength_IsRejected()
    {
        _validator.TestValidate(ValidRequest with { Description = new string('a', 501) })
            .ShouldHaveValidationErrorFor(x => x.Description).WithErrorMessage("Description cannot exceed 500 characters.");
    }

    [Fact]
    public void UndefinedStatus_IsRejected()
    {
        _validator.TestValidate(ValidRequest with { Status = (TaskItemStatus)99 })
            .ShouldHaveValidationErrorFor(x => x.Status).WithErrorMessage("Status is not valid.");
    }

    [Fact]
    public void PastDueDate_IsAllowedOnUpdate()
    {
        // Same rule as the domain: an overdue task must stay editable.
        _validator.TestValidate(ValidRequest with { DueDate = new DateOnly(2020, 1, 1) }).ShouldNotHaveAnyValidationErrors();
    }
}
