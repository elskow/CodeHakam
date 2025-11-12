using AccountService.DTOs;
using AccountService.Validation;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;

namespace AccountService.Tests.Unit.Validation;

public class UserSettingsValidationTests
{
    private static List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model);

        Validator.TryValidateObject(model, validationContext, validationResults, validateAllProperties: true);

        foreach (var property in model.GetType().GetProperties())
        {
            var propertyValue = property.GetValue(model);
            var propertyValidationContext = new ValidationContext(model) { MemberName = property.Name };
            var propertyValidationResults = new List<ValidationResult>();

            foreach (var attribute in property.GetCustomAttributes(typeof(ValidationAttribute), true).Cast<ValidationAttribute>())
            {
                var result = attribute.GetValidationResult(propertyValue, propertyValidationContext);
                if (result != ValidationResult.Success && result != null)
                {
                    if (!validationResults.Any(vr => vr.MemberNames.Contains(property.Name) && vr.ErrorMessage == result.ErrorMessage))
                    {
                        validationResults.Add(result);
                    }
                }
            }
        }

        return validationResults;
    }

    [Theory]
    [InlineData("light")]
    [InlineData("dark")]
    [InlineData("auto")]
    [InlineData("Light")]
    [InlineData("DARK")]
    [InlineData("AuTo")]
    public void UpdateUserSettingsRequest_WithValidTheme_ShouldPass(string theme)
    {
        var request = new UpdateUserSettingsRequest
        {
            Theme = theme
        };

        var validationResults = ValidateModel(request);

        validationResults.Should().BeEmpty();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("")]
    [InlineData("darkmode")]
    [InlineData("lightmode")]
    [InlineData("rainbow")]
    public void UpdateUserSettingsRequest_WithInvalidTheme_ShouldFail(string theme)
    {
        var request = new UpdateUserSettingsRequest
        {
            Theme = theme
        };

        var validationResults = ValidateModel(request);

        validationResults.Should().ContainSingle()
            .Which.ErrorMessage.Should().Contain("Theme must be one of");
    }

    [Theory]
    [InlineData("en")]
    [InlineData("id")]
    [InlineData("es")]
    [InlineData("fr")]
    [InlineData("de")]
    [InlineData("zh")]
    [InlineData("ja")]
    [InlineData("ko")]
    [InlineData("pt")]
    [InlineData("ru")]
    [InlineData("EN")]
    [InlineData("ID")]
    public void UpdateUserSettingsRequest_WithValidLanguage_ShouldPass(string language)
    {
        var request = new UpdateUserSettingsRequest
        {
            LanguagePreference = language
        };

        var validationResults = ValidateModel(request);

        validationResults.Should().BeEmpty();
    }

    [Theory]
    [InlineData("english")]
    [InlineData("")]
    [InlineData("xxx")]
    [InlineData("indonesia")]
    [InlineData("en-US")]
    public void UpdateUserSettingsRequest_WithInvalidLanguage_ShouldFail(string language)
    {
        var request = new UpdateUserSettingsRequest
        {
            LanguagePreference = language
        };

        var validationResults = ValidateModel(request);

        validationResults.Should().ContainSingle()
            .Which.ErrorMessage.Should().Contain("Language must be one of");
    }

    [Theory]
    [InlineData("public")]
    [InlineData("private")]
    [InlineData("friends")]
    [InlineData("Public")]
    [InlineData("PRIVATE")]
    [InlineData("FrIeNdS")]
    public void UpdateUserSettingsRequest_WithValidSolutionVisibility_ShouldPass(string visibility)
    {
        var request = new UpdateUserSettingsRequest
        {
            SolutionVisibility = visibility
        };

        var validationResults = ValidateModel(request);

        validationResults.Should().BeEmpty();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("")]
    [InlineData("everyone")]
    [InlineData("hidden")]
    [InlineData("only_me")]
    public void UpdateUserSettingsRequest_WithInvalidSolutionVisibility_ShouldFail(string visibility)
    {
        var request = new UpdateUserSettingsRequest
        {
            SolutionVisibility = visibility
        };

        var validationResults = ValidateModel(request);

        validationResults.Should().ContainSingle()
            .Which.ErrorMessage.Should().Contain("Solution visibility must be one of");
    }

    [Theory]
    [InlineData("UTC")]
    [InlineData("America/New_York")]
    [InlineData("Asia/Jakarta")]
    [InlineData("Europe/London")]
    [InlineData("Australia/Sydney")]
    [InlineData("Pacific/Auckland")]
    public void UpdateUserSettingsRequest_WithValidTimezone_ShouldPass(string timezone)
    {
        var request = new UpdateUserSettingsRequest
        {
            Timezone = timezone
        };

        var validationResults = ValidateModel(request);

        validationResults.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("Invalid/Timezone")]
    [InlineData("GMT")]
    [InlineData("EST")]
    [InlineData("PST")]
    [InlineData("America/NonExistent")]
    public void UpdateUserSettingsRequest_WithInvalidTimezone_ShouldFail(string timezone)
    {
        var request = new UpdateUserSettingsRequest
        {
            Timezone = timezone
        };

        var validationResults = ValidateModel(request);

        validationResults.Should().ContainSingle()
            .Which.ErrorMessage.Should().Contain("Invalid timezone");
    }

    [Fact]
    public void UpdateUserSettingsRequest_WithAllValidFields_ShouldPass()
    {
        var request = new UpdateUserSettingsRequest
        {
            LanguagePreference = "en",
            Theme = "dark",
            SolutionVisibility = "private",
            Timezone = "Asia/Jakarta",
            EmailNotifications = false,
            ContestReminders = true,
            ShowRating = true
        };

        var validationResults = ValidateModel(request);

        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void UpdateUserSettingsRequest_WithMultipleInvalidFields_ShouldReturnMultipleErrors()
    {
        var request = new UpdateUserSettingsRequest
        {
            LanguagePreference = "invalid_lang",
            Theme = "invalid_theme",
            SolutionVisibility = "invalid_visibility",
            Timezone = "Invalid/Zone"
        };

        var validationResults = ValidateModel(request);

        validationResults.Should().HaveCount(4);
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("Language"));
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("Theme"));
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("visibility"));
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("timezone"));
    }

    [Fact]
    public void ValidThemeAttribute_WithNullValue_ShouldPass()
    {
        var attribute = new ValidThemeAttribute();
        var result = attribute.IsValid(null);

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
    public void ValidSolutionVisibilityAttribute_WithNullValue_ShouldPass()
    {
        var attribute = new ValidSolutionVisibilityAttribute();
        var result = attribute.IsValid(null);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidTimezoneAttribute_WithNullValue_ShouldPass()
    {
        var attribute = new ValidTimezoneAttribute();
        var result = attribute.IsValid(null);

        result.Should().BeTrue();
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
}
