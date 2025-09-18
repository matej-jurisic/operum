using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Analytics.Requests
{
    public class CreateAnalyticRequiredDataTypeDto
    {
        public string Type { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
    }

    public class CreateAnalyticRequiredDataTypeDtoValidator : AbstractValidator<CreateAnalyticRequiredDataTypeDto>
    {
        public CreateAnalyticRequiredDataTypeDtoValidator()
        {
            RuleFor(x => x.Type)
                    .NotEmpty().WithMessage("Field type is required.")
                    .Must(DataTypes.IsValid)
                    .WithMessage($"Field type must be one of: {string.Join(", ", DataTypes.All)}");
        }
    }
}
