using System.Text.RegularExpressions;

namespace AccountService.Utilities;

public static class InputSanitizer
{
    private static readonly Regex NullByteRegex = new(@"\0", RegexOptions.Compiled);
    private static readonly Regex ControlCharsRegex = new(@"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", RegexOptions.Compiled);

    public static string? SanitizeSearchTerm(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        input = NullByteRegex.Replace(input, string.Empty);
        input = ControlCharsRegex.Replace(input, string.Empty);
        input = input.Trim();

        return string.IsNullOrWhiteSpace(input) ? null : input;
    }
}
