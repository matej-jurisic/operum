using FluentValidation;
using Operum.Model.Constants.Fields;

namespace Operum.Model.DTOs.TrackerConstants.Requests
{
    public class CreateTrackerConstantDto
    {
        public required string Name { get; set; } = string.Empty;
        public required string Type { get; set; } = string.Empty;
        public required string Value { get; set; } = string.Empty;
        public List<CreateTrackerConstantValueDto> Values { get; set; } = [];
    }

    public class CreateTrackerConstantDtoValidator : AbstractValidator<CreateTrackerConstantDto>
    {
        public CreateTrackerConstantDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Constant name is required.")
                .MaximumLength(30).WithMessage("Constant name cannot exceed 30 characters.");

            RuleFor(x => x.Type)
                .NotEmpty().WithMessage("Constant type is required.")
                .Must(DataTypes.CalculatedCompatible.Contains)
                .WithMessage("Constant type must be number, bool, or timespan.");

            RuleFor(x => x.Value)
                .NotEmpty().WithMessage("Constant value is required.");

            RuleFor(x => x.Value)
                .Must(v => double.TryParse(v, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _))
                .WithMessage("Value must be a valid number.")
                .When(x => x.Type == DataTypes.Number);

            RuleFor(x => x.Value)
                .Must(v => bool.TryParse(v, out _))
                .WithMessage("Value must be 'true' or 'false'.")
                .When(x => x.Type == DataTypes.Bool);

            RuleFor(x => x.Value)
                .Must(v => TimeSpan.TryParse(v, out _))
                .WithMessage("Value must be a valid timespan (e.g. 01:30:00).")
                .When(x => x.Type == DataTypes.TimeSpan);
        }
    }
}
