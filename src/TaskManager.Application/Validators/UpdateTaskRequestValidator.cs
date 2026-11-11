using FluentValidation;
using TaskManager.Application.Dtos.Tasks;

namespace TaskManager.Application.Validators;

public sealed class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Title).ValidTaskTitle();
        RuleFor(x => x.Description).ValidTaskDescription();
        RuleFor(x => x.Status).IsInEnum().WithMessage("Status is not valid.");
        // No due date rule: an overdue task must stay editable (same as the domain).
    }
}
