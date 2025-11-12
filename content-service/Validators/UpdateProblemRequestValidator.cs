using ContentService.DTOs.Requests;
using ContentService.Enums;
using FluentValidation;

namespace ContentService.Validators;

public class UpdateProblemRequestValidator : AbstractValidator<UpdateProblemRequest>
{
    public UpdateProblemRequestValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(200).When(x => x.Title != null)
            .WithMessage("Title cannot exceed 200 characters.")
            .MinimumLength(5).When(x => x.Title != null)
            .WithMessage("Title must be at least 5 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(50000).When(x => x.Description != null)
            .WithMessage("Description cannot exceed 50000 characters.")
            .MinimumLength(20).When(x => x.Description != null)
            .WithMessage("Description must be at least 20 characters.");

        RuleFor(x => x.InputFormat)
            .MaximumLength(5000).When(x => x.InputFormat != null)
            .WithMessage("Input format cannot exceed 5000 characters.");

        RuleFor(x => x.OutputFormat)
            .MaximumLength(5000).When(x => x.OutputFormat != null)
            .WithMessage("Output format cannot exceed 5000 characters.");

        RuleFor(x => x.Constraints)
            .MaximumLength(5000).When(x => x.Constraints != null)
            .WithMessage("Constraints cannot exceed 5000 characters.");

        RuleFor(x => x.Difficulty)
            .Must(BeValidDifficulty).When(x => x.Difficulty != null)
            .WithMessage("Difficulty must be one of: Easy, Medium, Hard.");

        RuleFor(x => x.TimeLimit)
            .GreaterThan(0).When(x => x.TimeLimit.HasValue)
            .WithMessage("Time limit must be greater than 0.")
            .LessThanOrEqualTo(30000).When(x => x.TimeLimit.HasValue)
            .WithMessage("Time limit cannot exceed 30 seconds (30000 ms).");

        RuleFor(x => x.MemoryLimit)
            .GreaterThan(0).When(x => x.MemoryLimit.HasValue)
            .WithMessage("Memory limit must be greater than 0.")
            .LessThanOrEqualTo(1024).When(x => x.MemoryLimit.HasValue)
            .WithMessage("Memory limit cannot exceed 1024 MB.");

        RuleFor(x => x.Tags)
            .Must(tags => tags!.Count <= 10).When(x => x.Tags != null)
            .WithMessage("Cannot have more than 10 tags.")
            .Must(tags => tags!.All(tag => !string.IsNullOrWhiteSpace(tag) && tag.Length <= 50))
            .When(x => x.Tags != null)
            .WithMessage("Each tag must be non-empty and not exceed 50 characters.");

        RuleFor(x => x.Visibility)
            .Must(BeValidVisibility).When(x => !string.IsNullOrEmpty(x.Visibility))
            .WithMessage("Visibility must be one of: Public, Private, ContestOnly.");

        RuleFor(x => x.HintText)
            .MaximumLength(2000).When(x => !string.IsNullOrEmpty(x.HintText))
            .WithMessage("Hint text cannot exceed 2000 characters.");
    }

    private bool BeValidDifficulty(string? difficulty)
    {
        if (string.IsNullOrEmpty(difficulty))
        {
            return true;
        }

        return Enum.TryParse<Difficulty>(difficulty, ignoreCase: true, out _);
    }

    private bool BeValidVisibility(string? visibility)
    {
        if (string.IsNullOrEmpty(visibility))
        {
            return true;
        }

        return Enum.TryParse<ProblemVisibility>(visibility, ignoreCase: true, out _);
    }
}
