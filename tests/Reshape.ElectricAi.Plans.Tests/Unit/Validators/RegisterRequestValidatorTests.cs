using FluentValidation.TestHelper;
using Reshape.ElectricAi.Core.Dtos.Auth;
using Reshape.ElectricAi.Plans.Validators;

namespace Reshape.ElectricAi.Plans.Tests.Unit.Validators;

public sealed class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public void Validate_PasswordTooShort_Fails()
    {
        var result = _validator.TestValidate(new RegisterRequest("alice@example.com", "Short1!"));

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_PasswordMissingDigit_Fails()
    {
        var result = _validator.TestValidate(new RegisterRequest("alice@example.com", "NoDigitsHere!"));

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_PasswordMissingSymbol_Fails()
    {
        var result = _validator.TestValidate(new RegisterRequest("alice@example.com", "NoSymbolHere1"));

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_EmailInvalid_Fails()
    {
        var result = _validator.TestValidate(new RegisterRequest("not-an-email", "ValidPass1!"));

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_ValidInput_Passes()
    {
        var result = _validator.TestValidate(new RegisterRequest("alice@example.com", "ValidPass1!"));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
