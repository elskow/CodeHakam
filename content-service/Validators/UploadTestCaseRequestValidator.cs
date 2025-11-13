using ContentService.Constants;
using ContentService.DTOs.Requests;
using FluentValidation;

namespace ContentService.Validators;

public class UploadTestCaseRequestValidator : AbstractValidator<UploadTestCaseRequest>
{
    public UploadTestCaseRequestValidator()
    {
        RuleFor(x => x.InputFile)
            .NotNull().WithMessage("Input file is required.")
            .Must(file => file.Length > 0).WithMessage("Input file cannot be empty.")
            .Must(file => file.Length <= ApplicationConstants.Limits.MaxTestCaseFileSizeBytes)
            .WithMessage($"Input file size cannot exceed {ApplicationConstants.Validation.MaxTestCaseFileSizeMb} MB.");

        RuleFor(x => x.OutputFile)
            .NotNull().WithMessage("Output file is required.")
            .Must(file => file.Length > 0).WithMessage("Output file cannot be empty.")
            .Must(file => file.Length <= ApplicationConstants.Limits.MaxTestCaseFileSizeBytes)
            .WithMessage($"Output file size cannot exceed {ApplicationConstants.Validation.MaxTestCaseFileSizeMb} MB.");

        RuleFor(x => x.TestNumber)
            .GreaterThan(0).WithMessage("Test number must be greater than 0.")
            .LessThanOrEqualTo(ApplicationConstants.Limits.MaxTestNumber)
            .WithMessage($"Test number cannot exceed {ApplicationConstants.Limits.MaxTestNumber}.");
    }
}
