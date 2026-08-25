using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.Constants.Analytics;

namespace Operum.Model.DTOs.Analytics.Requests
{
    public class CreateAnalyticDto
    {
        public required string Code { get; set; } = string.Empty;
        public required string Type { get; set; } = string.Empty;
        // Optional: left unset, the analytic falls back to its definition's own label
        // (e.g. "Sum") the way it always has.
        public string? Name { get; set; }
        public List<CreateAnalyticFieldDto> AnalyticFields { get; set; } = [];
    }

    public class CreateAnalyticDtoValidator : AbstractValidator<CreateAnalyticDto>
    {
        public CreateAnalyticDtoValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage((x) => Messages.Required("code"))
                .Must(AnalyticCodes.IsValid).WithMessage((x) => Messages.Invalid("code"));

            RuleFor(x => x.Type)
               .NotEmpty().WithMessage((x) => Messages.Required("type"))
               .Must(AnalyticTypes.IsValid).WithMessage((x) => Messages.Invalid("type"));

            RuleFor(x => x.Name)
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.")
                .When(x => !string.IsNullOrEmpty(x.Name));

            RuleForEach(x => x.AnalyticFields)
                .SetValidator(new CreateAnalyticFieldDtoValidator());
        }
    }
}
