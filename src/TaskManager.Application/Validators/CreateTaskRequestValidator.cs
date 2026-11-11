using FluentValidation;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Dtos.Tasks;

namespace TaskManager.Application.Validators;

public sealed class CreateTaskRequestValidator(IClock clock) : AbstractValidator<CreateTaskRequest>;
