using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.Constants.Analytics;

namespace Operum.Model.DTOs.Analytics.Requests
{
    public class CreateAnalyticFieldDto
    {
        public string FieldId { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
    }

    public class CreateAnalyticFieldDtoValidator : AbstractValidator<CreateAnalyticFieldDto>
    {
        public CreateAnalyticFieldDtoValidator()
        {
            RuleFor(x => x.FieldId)
                .NotEmpty().WithMessage(Messages.Required("field id"));

            RuleFor(x => x.Purpose)
               .NotEmpty().WithMessage(Messages.Required("purpose"))
               .Must(AnalyticPurposes.IsValid).WithMessage(Messages.Invalid("purpose"));
        }
    }
}
