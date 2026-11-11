using FluentValidation;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Validators;

/// <summary>Rules shared by the create and update task validators.</summary>
internal static class TaskRules
{
    public static IRuleBuilderOptions<T, string> ValidTaskTitle<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Title is required.")
            .MaximumLength(TaskItem.TitleMaxLength).WithMessage($"Title cannot exceed {TaskItem.TitleMaxLength} characters.");

    public static IRuleBuilderOptions<T, string?> ValidTaskDescription<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(TaskItem.DescriptionMaxLength)
            .WithMessage($"Description cannot exceed {TaskItem.DescriptionMaxLength} characters.");
}
