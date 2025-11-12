using ContentService.DTOs.Requests;
using FluentValidation;

namespace ContentService.Validators;

public class UploadTestCaseRequestValidator : AbstractValidator<UploadTestCaseRequest>
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public UploadTestCaseRequestValidator()
    {
        RuleFor(x => x.ProblemId)
            .GreaterThan(0).WithMessage("Problem ID must be greater than 0.");

        RuleFor(x => x.InputFile)
            .NotNull().WithMessage("Input file is required.")
            .Must(file => file.Length > 0).WithMessage("Input file cannot be empty.")
            .Must(file => file.Length <= MaxFileSizeBytes)
            .WithMessage($"Input file size cannot exceed {MaxFileSizeBytes / (1024 * 1024)} MB.");

        RuleFor(x => x.OutputFile)
            .NotNull().WithMessage("Output file is required.")
            .Must(file => file.Length > 0).WithMessage("Output file cannot be empty.")
            .Must(file => file.Length <= MaxFileSizeBytes)
            .WithMessage($"Output file size cannot exceed {MaxFileSizeBytes / (1024 * 1024)} MB.");

        RuleFor(x => x.TestNumber)
            .GreaterThan(0).WithMessage("Test number must be greater than 0.")
            .LessThanOrEqualTo(1000).WithMessage("Test number cannot exceed 1000.");
    }
}
