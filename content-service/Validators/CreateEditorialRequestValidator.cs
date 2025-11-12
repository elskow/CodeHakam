using ContentService.DTOs.Requests;
using FluentValidation;

namespace ContentService.Validators;

public class CreateEditorialRequestValidator : AbstractValidator<CreateEditorialRequest>
{
    public CreateEditorialRequestValidator()
    {
        RuleFor(x => x.ProblemId)
            .GreaterThan(0).WithMessage("Problem ID must be greater than 0.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.")
            .MinimumLength(50).WithMessage("Content must be at least 50 characters.")
            .MaximumLength(100000).WithMessage("Content cannot exceed 100000 characters.");

        RuleFor(x => x.TimeComplexity)
            .NotEmpty().WithMessage("Time complexity is required.")
            .MaximumLength(100).WithMessage("Time complexity cannot exceed 100 characters.")
            .Must(BeValidComplexity).WithMessage("Time complexity must be in valid Big-O notation (e.g., O(n), O(log n)).");

        RuleFor(x => x.SpaceComplexity)
            .NotEmpty().WithMessage("Space complexity is required.")
            .MaximumLength(100).WithMessage("Space complexity cannot exceed 100 characters.")
            .Must(BeValidComplexity).WithMessage("Space complexity must be in valid Big-O notation (e.g., O(1), O(n)).");

        RuleFor(x => x.VideoUrl)
            .Must(BeValidUrl).When(x => !string.IsNullOrEmpty(x.VideoUrl))
            .WithMessage("Video URL must be a valid URL.");
    }

    private bool BeValidComplexity(string complexity)
    {
        if (string.IsNullOrWhiteSpace(complexity))
        {
            return false;
        }

        // Basic validation for Big-O notation
        var trimmed = complexity.Trim();
        return trimmed.StartsWith("O(") && trimmed.EndsWith(")") && trimmed.Length > 3;
    }

    private bool BeValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return true;
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
