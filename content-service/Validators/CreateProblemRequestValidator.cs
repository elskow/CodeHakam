using ContentService.DTOs.Requests;
using ContentService.Enums;
using FluentValidation;

namespace ContentService.Validators;

public class CreateProblemRequestValidator : AbstractValidator<CreateProblemRequest>
{
    public CreateProblemRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.")
            .MinimumLength(5).WithMessage("Title must be at least 5 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(50000).WithMessage("Description cannot exceed 50000 characters.")
            .MinimumLength(20).WithMessage("Description must be at least 20 characters.");


        RuleFor(x => x.InputFormat)
            .NotEmpty().WithMessage("Input format is required.")
            .MaximumLength(5000).WithMessage("Input format cannot exceed 5000 characters.");

        RuleFor(x => x.OutputFormat)
            .NotEmpty().WithMessage("Output format is required.")
            .MaximumLength(5000).WithMessage("Output format cannot exceed 5000 characters.");

        RuleFor(x => x.Constraints)
            .NotEmpty().WithMessage("Constraints are required.")
            .MaximumLength(5000).WithMessage("Constraints cannot exceed 5000 characters.");

        RuleFor(x => x.Difficulty)
            .IsInEnum().WithMessage("Difficulty must be one of: Easy, Medium, Hard.");

        RuleFor(x => x.TimeLimit)
            .GreaterThan(0).WithMessage("Time limit must be greater than 0.")
            .LessThanOrEqualTo(30000).WithMessage("Time limit cannot exceed 30 seconds (30000 ms).");

        RuleFor(x => x.MemoryLimit)
            .GreaterThan(0).WithMessage("Memory limit must be greater than 0.")
            .LessThanOrEqualTo(1024).WithMessage("Memory limit cannot exceed 1024 MB.");

        RuleFor(x => x.Tags)
            .NotNull().WithMessage("Tags cannot be null.")
            .Must(tags => tags.Count <= 10).WithMessage("Cannot have more than 10 tags.")
            .Must(tags => tags.All(tag => !string.IsNullOrWhiteSpace(tag) && tag.Length <= 50))
            .WithMessage("Each tag must be non-empty and not exceed 50 characters.");

        RuleFor(x => x.Visibility)
            .IsInEnum().When(x => x.Visibility.HasValue)
            .WithMessage("Visibility must be one of: Public, Private, ContestOnly.");

        RuleFor(x => x.HintText)
            .MaximumLength(2000).When(x => !string.IsNullOrEmpty(x.HintText))
            .WithMessage("Hint text cannot exceed 2000 characters.");
    }

    
}
