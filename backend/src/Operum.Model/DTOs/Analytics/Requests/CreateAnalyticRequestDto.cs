using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.Enums;

namespace Operum.Model.DTOs.Analytics.Requests
{
    public class CreateAnalyticRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ResultType { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int AnalyticTypeId { get; set; }
        public List<List<CreateAnalyticRequiredDataTypeDto>> AnalyticRequiredDataTypesList { get; set; } = [];
    }

    public class CreateAnalyticRequestDtoValidator : AbstractValidator<CreateAnalyticRequestDto>
    {
        public CreateAnalyticRequestDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Analytic name is required")
                .MaximumLength(100).WithMessage("Analytic name cannot exceed 100 characters.");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Analytic code is required")
                .MaximumLength(100).WithMessage("Analytic code cannot exceed 100 characters.");

            RuleFor(x => x.ResultType)
                .NotEmpty().WithMessage("Result type is required.")
                .Must(AnalyticResultTypes.IsValid)
                .WithMessage("Result type is invalid.");

            RuleFor(x => x.AnalyticTypeId)
                .NotNull().WithMessage("Analytic type is required.")
                .Must(x => Enum.IsDefined(typeof(AnalyticTypeEnum), x))
                .WithMessage("Analytic type is invalid.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.")
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleForEach(x => x.AnalyticRequiredDataTypesList)
                .ChildRules(inner =>
                {
                    inner.RuleForEach(y => y)
                        .SetValidator(new CreateAnalyticRequiredDataTypeDtoValidator());
                });
        }
    }
}
