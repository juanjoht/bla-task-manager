using FluentValidation;
using TaskManager.Application.Dtos.Tasks;

namespace TaskManager.Application.Validators;

public sealed class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>;
