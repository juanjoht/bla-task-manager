using FluentValidation.TestHelper;
using TaskManager.Application.Dtos.Auth;
using TaskManager.Application.Validators;

namespace TaskManager.Application.Tests.Validators;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_HasNoErrors()
    {
        _validator.TestValidate(new LoginRequest("demo@ballastlane.test", "Demo123!")).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void MissingEmail_IsRejected()
    {
        _validator.TestValidate(new LoginRequest("", "Demo123!"))
            .ShouldHaveValidationErrorFor(x => x.Email).WithErrorMessage("Email is required.");
    }

    [Fact]
    public void MissingPassword_IsRejected()
    {
        _validator.TestValidate(new LoginRequest("demo@ballastlane.test", ""))
            .ShouldHaveValidationErrorFor(x => x.Password).WithErrorMessage("Password is required.");
    }

    [Fact]
    public void WeakPassword_IsNotCheckedOnLogin()
    {
        // Password strength is a registration rule; on login it would only leak hints to an attacker.
        _validator.TestValidate(new LoginRequest("demo@ballastlane.test", "x")).ShouldNotHaveAnyValidationErrors();
    }
}
