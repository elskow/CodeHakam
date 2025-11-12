using ContentService.DTOs.Requests;
using FluentValidation;

namespace ContentService.Validators;

public class AddCommentRequestValidator : AbstractValidator<AddCommentRequest>
{
    public AddCommentRequestValidator()
    {
        RuleFor(x => x.DiscussionId)
            .GreaterThan(0).WithMessage("Discussion ID must be greater than 0.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.")
            .MinimumLength(1).WithMessage("Content must be at least 1 character.")
            .MaximumLength(5000).WithMessage("Content cannot exceed 5000 characters.");

        RuleFor(x => x.ParentId)
            .GreaterThan(0).When(x => x.ParentId.HasValue)
            .WithMessage("Parent ID must be greater than 0 when provided.");
    }
}
