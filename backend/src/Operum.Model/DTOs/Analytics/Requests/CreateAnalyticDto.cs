using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.Constants.Analytics;

namespace Operum.Model.DTOs.Analytics.Requests
{
    public class CreateAnalyticDto
    {
        public string Code { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public List<CreateAnalyticFieldDto> AnalyticFields { get; set; } = [];
    }

    public class CreateAnalyticDtoValidator : AbstractValidator<CreateAnalyticDto>
    {
        public CreateAnalyticDtoValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage(Messages.Required("code"))
                .Must(AnalyticCodes.IsValid).WithMessage(Messages.Invalid("code"));

            RuleFor(x => x.Type)
               .NotEmpty().WithMessage(Messages.Required("type"))
               .Must(AnalyticTypes.IsValid).WithMessage(Messages.Invalid("type"));
        }
    }
}
