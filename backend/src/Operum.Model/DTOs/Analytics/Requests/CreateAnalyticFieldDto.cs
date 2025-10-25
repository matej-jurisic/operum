using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.Constants.Analytics;

namespace Operum.Model.DTOs.Analytics.Requests
{
    public class CreateAnalyticFieldDto
    {
        public required string FieldId { get; set; } = string.Empty;
        public required string Purpose { get; set; } = string.Empty;
    }

    public class CreateAnalyticFieldDtoValidator : AbstractValidator<CreateAnalyticFieldDto>
    {
        public CreateAnalyticFieldDtoValidator()
        {
            RuleFor(x => x.FieldId)
                .NotEmpty().WithMessage((x) => Messages.Required("field id"));

            RuleFor(x => x.Purpose)
               .NotEmpty().WithMessage((x) => Messages.Required("purpose"))
               .Must(AnalyticPurposes.IsValid).WithMessage((x) => Messages.Invalid("purpose"));
        }
    }
}
