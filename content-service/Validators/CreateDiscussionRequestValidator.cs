using ContentService.DTOs.Requests;
using FluentValidation;

namespace ContentService.Validators;

public class CreateDiscussionRequestValidator : AbstractValidator<CreateDiscussionRequest>
{
    public CreateDiscussionRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MinimumLength(5).WithMessage("Title must be at least 5 characters.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.")
            .MinimumLength(10).WithMessage("Content must be at least 10 characters.")
            .MaximumLength(10000).WithMessage("Content cannot exceed 10000 characters.");
    }
}
