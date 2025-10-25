using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.Constants.Analytics;

namespace Operum.Model.DTOs.Analytics.Requests
{
    public class CreateAnalyticDto
    {
        public required string Code { get; set; } = string.Empty;
        public required string Type { get; set; } = string.Empty;
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

            RuleForEach(x => x.AnalyticFields)
                .SetValidator(new CreateAnalyticFieldDtoValidator());
        }
    }
}
