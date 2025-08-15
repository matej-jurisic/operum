using FluentValidation;

namespace Operum.Model.DTOs.Analytics.Requests
{
    public class CreateAnalyticRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<CreateAnalyticRequiredDataTypeDto> AnalyticRequiredDataTypes { get; set; } = [];
    }

    public class CreateAnalyticRequestDtoValidator : AbstractValidator<CreateAnalyticRequestDto>
    {
        public CreateAnalyticRequestDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Analytic name is required")
                .MaximumLength(100).WithMessage("Analytic name cannot exceed 100 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.")
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.AnalyticRequiredDataTypes)
                .NotEmpty().WithMessage("At least one data type is required.");
        }
    }
}
