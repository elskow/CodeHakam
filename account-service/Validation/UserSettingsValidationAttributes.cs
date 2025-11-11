using System.ComponentModel.DataAnnotations;
using AccountService.Enums;

namespace AccountService.Validation;

/// <summary>
/// Validates that a string value corresponds to a valid Theme enum value (case-insensitive)
/// </summary>
public class ValidThemeAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return ValidationResult.Success; // Allow null for optional fields
        }

        if (value is not string strValue)
        {
            return new ValidationResult("Theme must be a string");
        }

        if (Enum.TryParse<Theme>(strValue, ignoreCase: true, out _))
        {
            return ValidationResult.Success;
        }

        var validValues = string.Join(", ", Enum.GetNames(typeof(Theme)).Select(v => v.ToLower()));
        return new ValidationResult($"Invalid theme. Valid values are: {validValues}");
    }
}

/// <summary>
/// Validates that a string value corresponds to a valid SolutionVisibility enum value (case-insensitive)
/// </summary>
public class ValidSolutionVisibilityAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return ValidationResult.Success; // Allow null for optional fields
        }

        if (value is not string strValue)
        {
            return new ValidationResult("Solution visibility must be a string");
        }

        if (string.IsNullOrWhiteSpace(strValue))
        {
            return new ValidationResult("Solution visibility cannot be empty");
        }

        if (Enum.TryParse<SolutionVisibility>(strValue, ignoreCase: true, out _))
        {
            return ValidationResult.Success;
        }

        var validValues = string.Join(", ", Enum.GetNames(typeof(SolutionVisibility)).Select(v => v.ToLower()));
        return new ValidationResult($"Invalid solution visibility. Valid values are: {validValues}");
    }
}

/// <summary>
/// Validates that a string value corresponds to a valid LanguagePreference enum value (case-insensitive)
/// </summary>
public class ValidLanguagePreferenceAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return ValidationResult.Success; // Allow null for optional fields
        }

        if (value is not string strValue)
        {
            return new ValidationResult("Language preference must be a string");
        }

        if (Enum.TryParse<LanguagePreference>(strValue, ignoreCase: true, out _))
        {
            return ValidationResult.Success;
        }

        var validValues = string.Join(", ", Enum.GetNames(typeof(LanguagePreference)).Select(v => v.ToLower()));
        return new ValidationResult($"Invalid language preference. Valid values are: {validValues}");
    }
}

/// <summary>
/// Validates that a string value is a valid IANA timezone identifier
/// </summary>
public class ValidTimezoneAttribute : ValidationAttribute
{
    private static readonly HashSet<string> ValidTimezones = TimeZoneInfo.GetSystemTimeZones()
        .Select(tz => tz.Id)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return ValidationResult.Success; // Allow null for optional fields
        }

        if (value is not string strValue)
        {
            return new ValidationResult("Timezone must be a string");
        }

        if (ValidTimezones.Contains(strValue))
        {
            return ValidationResult.Success;
        }

        // Also allow "UTC" as a special case
        if (strValue.Equals("UTC", StringComparison.OrdinalIgnoreCase))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult($"Invalid timezone. Please provide a valid IANA timezone identifier (e.g., 'UTC', 'America/New_York', 'Asia/Jakarta')");
    }
}
