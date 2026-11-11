using FluentValidation;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Dtos.Tasks;

namespace TaskManager.Application.Validators;

public sealed class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator(IClock clock)
    {
        RuleFor(x => x.Title).ValidTaskTitle();
        RuleFor(x => x.Description).ValidTaskDescription();

        // A lambda so "today" is read on every validation, not once when the validator is built.
        RuleFor(x => x.DueDate)
            .Must(dueDate => dueDate >= clock.Today).WithMessage("Due date cannot be in the past.");
    }
}
