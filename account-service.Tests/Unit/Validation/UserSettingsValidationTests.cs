using AccountService.DTOs;
using AccountService.Validators;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;

namespace AccountService.Tests.Unit.Validation;

public class UserSettingsValidationTests
{
    [Fact]
    public void ValidThemeAttribute_WithNullValue_ShouldPass()
    {
        var attribute = new ValidThemeAttribute();
        var result = attribute.IsValid(null);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidThemeAttribute_WithValidValue_ShouldPass()
    {
        var attribute = new ValidThemeAttribute();

        attribute.IsValid("light").Should().BeTrue();
        attribute.IsValid("dark").Should().BeTrue();
        attribute.IsValid("auto").Should().BeTrue();
    }

    [Fact]
    public void ValidThemeAttribute_WithInvalidValue_ShouldFail()
    {
        var attribute = new ValidThemeAttribute();
        var context = new ValidationContext(new object());

        var result = attribute.GetValidationResult("invalid", context);

        result.Should().NotBe(ValidationResult.Success);
        result!.ErrorMessage.Should().Contain("Theme must be one of");
    }

    [Fact]
    public void ValidThemeAttribute_WithEmptyString_ShouldFail()
    {
        var attribute = new ValidThemeAttribute();
        var context = new ValidationContext(new object());

        var result = attribute.GetValidationResult("", context);

        result.Should().NotBe(ValidationResult.Success);
        result!.ErrorMessage.Should().Contain("cannot be empty");
    }

    [Theory]
    [InlineData("light")]
    [InlineData("dark")]
    [InlineData("auto")]
    [InlineData("Light")]
    [InlineData("Dark")]
    [InlineData("Auto")]
    public void ValidThemeAttribute_ShouldBeCaseInsensitive(string input)
    {
        var attribute = new ValidThemeAttribute();
        var result = attribute.IsValid(input);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidLanguagePreferenceAttribute_WithNullValue_ShouldPass()
    {
        var attribute = new ValidLanguagePreferenceAttribute();
        var result = attribute.IsValid(null);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidLanguagePreferenceAttribute_WithValidValue_ShouldPass()
    {
        var attribute = new ValidLanguagePreferenceAttribute();

        attribute.IsValid("en").Should().BeTrue();
        attribute.IsValid("id").Should().BeTrue();
        attribute.IsValid("es").Should().BeTrue();
    }

    [Fact]
    public void ValidLanguagePreferenceAttribute_WithInvalidValue_ShouldFail()
    {
        var attribute = new ValidLanguagePreferenceAttribute();
        var context = new ValidationContext(new object());

        var result = attribute.GetValidationResult("invalid", context);

        result.Should().NotBe(ValidationResult.Success);
        result!.ErrorMessage.Should().Contain("Language must be one of");
    }

    [Fact]
    public void ValidLanguagePreferenceAttribute_WithEmptyString_ShouldFail()
    {
        var attribute = new ValidLanguagePreferenceAttribute();
        var context = new ValidationContext(new object());

        var result = attribute.GetValidationResult("", context);

        result.Should().NotBe(ValidationResult.Success);
        result!.ErrorMessage.Should().Contain("cannot be empty");
    }

    [Theory]
    [InlineData("en")]
    [InlineData("id")]
    [InlineData("es")]
    [InlineData("EN")]
    [InlineData("ID")]
    [InlineData("ES")]
    public void ValidLanguagePreferenceAttribute_ShouldBeCaseInsensitive(string input)
    {
        var attribute = new ValidLanguagePreferenceAttribute();
        var result = attribute.IsValid(input);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidSolutionVisibilityAttribute_WithNullValue_ShouldPass()
    {
        var attribute = new ValidSolutionVisibilityAttribute();
        var result = attribute.IsValid(null);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidSolutionVisibilityAttribute_WithValidValue_ShouldPass()
    {
        var attribute = new ValidSolutionVisibilityAttribute();

        attribute.IsValid("public").Should().BeTrue();
        attribute.IsValid("private").Should().BeTrue();
        attribute.IsValid("friends").Should().BeTrue();
    }

    [Fact]
    public void ValidSolutionVisibilityAttribute_WithInvalidValue_ShouldFail()
    {
        var attribute = new ValidSolutionVisibilityAttribute();
        var context = new ValidationContext(new object());

        var result = attribute.GetValidationResult("invalid", context);

        result.Should().NotBe(ValidationResult.Success);
        result!.ErrorMessage.Should().Contain("Solution visibility must be one of");
    }

    [Fact]
    public void ValidSolutionVisibilityAttribute_WithEmptyString_ShouldFail()
    {
        var attribute = new ValidSolutionVisibilityAttribute();
        var context = new ValidationContext(new object());

        var result = attribute.GetValidationResult("", context);

        result.Should().NotBe(ValidationResult.Success);
        result!.ErrorMessage.Should().Contain("cannot be empty");
    }

    [Theory]
    [InlineData("public")]
    [InlineData("private")]
    [InlineData("friends")]
    [InlineData("Public")]
    [InlineData("Private")]
    [InlineData("Friends")]
    public void ValidSolutionVisibilityAttribute_ShouldBeCaseInsensitive(string input)
    {
        var attribute = new ValidSolutionVisibilityAttribute();
        var result = attribute.IsValid(input);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidTimezoneAttribute_WithNullValue_ShouldPass()
    {
        var attribute = new ValidTimezoneAttribute();
        var result = attribute.IsValid(null);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidTimezoneAttribute_WithValidValue_ShouldPass()
    {
        var attribute = new ValidTimezoneAttribute();

        attribute.IsValid("UTC").Should().BeTrue();
        attribute.IsValid("America/New_York").Should().BeTrue();
        attribute.IsValid("Asia/Jakarta").Should().BeTrue();
    }

    [Fact]
    public void ValidTimezoneAttribute_WithInvalidValue_ShouldFail()
    {
        var attribute = new ValidTimezoneAttribute();
        var context = new ValidationContext(new object());

        var result = attribute.GetValidationResult("GMT", context);

        result.Should().NotBe(ValidationResult.Success);
        result!.ErrorMessage.Should().Contain("Invalid timezone");
    }

    [Fact]
    public void ValidTimezoneAttribute_WithEmptyString_ShouldFail()
    {
        var attribute = new ValidTimezoneAttribute();
        var context = new ValidationContext(new object());

        var result = attribute.GetValidationResult("", context);

        result.Should().NotBe(ValidationResult.Success);
        result!.ErrorMessage.Should().Contain("cannot be empty");
    }
}
