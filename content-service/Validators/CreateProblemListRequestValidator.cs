using ContentService.DTOs.Requests;
using FluentValidation;

namespace ContentService.Validators;

public class CreateProblemListRequestValidator : AbstractValidator<CreateProblemListRequest>
{
    public CreateProblemListRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MinimumLength(3).WithMessage("Name must be at least 3 characters.")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Description cannot exceed 1000 characters.");

        RuleFor(x => x.ProblemIds)
            .NotNull().WithMessage("Problem IDs cannot be null.")
            .Must(ids => ids.Count <= 100).WithMessage("Cannot add more than 100 problems to a list.")
            .Must(ids => ids.All(id => id > 0)).WithMessage("All problem IDs must be greater than 0.")
            .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("Problem IDs must be unique.");
    }
}
