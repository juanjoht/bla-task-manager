using FluentValidation;
using TaskManager.Application.Dtos.Auth;

namespace TaskManager.Application.Validators;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>;
