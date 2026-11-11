using FluentValidation.TestHelper;
using TaskManager.Application.Dtos.Auth;
using TaskManager.Application.Validators;

namespace TaskManager.Application.Tests.Validators;

public class RegisterRequestValidatorTests
{
    private static readonly RegisterRequest ValidRequest = new("demo@ballastlane.test", "Demo User", "Demo123!");

    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_HasNoErrors()
    {
        _validator.TestValidate(ValidRequest).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingEmail_IsRejected(string email)
    {
        _validator.TestValidate(ValidRequest with { Email = email })
            .ShouldHaveValidationErrorFor(x => x.Email).WithErrorMessage("Email is required.");
    }

    [Theory]
    [InlineData("plainaddress")]
    [InlineData("user@domain")]
    [InlineData("us er@domain.com")]
    public void InvalidEmailFormat_IsRejected(string email)
    {
        _validator.TestValidate(ValidRequest with { Email = email })
            .ShouldHaveValidationErrorFor(x => x.Email).WithErrorMessage("Email format is not valid.");
    }

    [Fact]
    public void MissingName_IsRejected()
    {
        _validator.TestValidate(ValidRequest with { Name = "" })
            .ShouldHaveValidationErrorFor(x => x.Name).WithErrorMessage("Name is required.");
    }

    [Fact]
    public void NameOverMaxLength_IsRejected()
    {
        _validator.TestValidate(ValidRequest with { Name = new string('a', 101) })
            .ShouldHaveValidationErrorFor(x => x.Name).WithErrorMessage("Name cannot exceed 100 characters.");
    }

    [Theory]
    [InlineData("Ab1", "Password must be at least 8 characters.")]
    [InlineData("demo1234", "Password must contain an uppercase letter.")]
    [InlineData("DEMO1234", "Password must contain a lowercase letter.")]
    [InlineData("DemoDemo", "Password must contain a number.")]
    public void WeakPassword_IsRejected(string password, string expectedMessage)
    {
        _validator.TestValidate(ValidRequest with { Password = password })
            .ShouldHaveValidationErrorFor(x => x.Password).WithErrorMessage(expectedMessage);
    }

    [Fact]
    public void SeveralInvalidFields_AreAllReportedAtOnce()
    {
        var result = _validator.TestValidate(new RegisterRequest("", "", ""));

        result.ShouldHaveValidationErrorFor(x => x.Email);
        result.ShouldHaveValidationErrorFor(x => x.Name);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}
