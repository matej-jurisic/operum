using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.Constants.Fields;
using Operum.Model.Converters;
using System.Globalization;

namespace Operum.Model.DTOs.Fields.Requests
{
    public class CreateFieldDto
    {
        public required string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public required string Type { get; set; } = string.Empty;
        public bool Required { get; set; } = false;
        public List<string>? SelectOptions { get; set; }
        public bool IsCalculated { get; set; } = false;
        public string? Formula { get; set; }
        public string? ReferencedTrackerId { get; set; }
        public string? ReferencedDisplayFieldId { get; set; }
        public string? DefaultValue { get; set; }
        public string? DefaultValueConstantId { get; set; }
        public string? VisibilityFieldId { get; set; }
        public string? VisibilityOperator { get; set; }
        public string? VisibilityValue { get; set; }
    }

    public class CreateFieldDtoValidator : AbstractValidator<CreateFieldDto>
    {
        public CreateFieldDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage((x) => Messages.Required("field name"))
                .MaximumLength(30).WithMessage("Field name cannot exceed 30 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.")
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.Type)
                .NotEmpty().WithMessage((x) => Messages.Required("field type"))
                .Must(DataTypes.IsValid).WithMessage((x) => Messages.Invalid("field type"));

            RuleFor(x => x.Formula)
                .NotEmpty().WithMessage("Formula is required for calculated fields.")
                .MaximumLength(500).WithMessage("Formula cannot exceed 500 characters.")
                .When(x => x.IsCalculated);

            RuleFor(x => x.Formula)
                .Empty().WithMessage("Formula must be empty for manual fields.")
                .When(x => !x.IsCalculated);

            RuleFor(x => x.Required)
                .Equal(false).WithMessage("Calculated fields cannot be required.")
                .When(x => x.IsCalculated);

            RuleFor(x => x.Type)
                .Must(DataTypes.CalculatedCompatible.Contains)
                .WithMessage("Calculated field type must be number, bool, or timespan.")
                .When(x => x.IsCalculated);

            RuleForEach(x => x.SelectOptions)
                .NotEmpty().WithMessage("Select option cannot be empty.")
                .MaximumLength(1000).WithMessage("Select option cannot exceed 1000 characters.")
                .When(x => x.SelectOptions != null);

            RuleForEach(x => x.SelectOptions)
                .Must(opt => double.TryParse(opt, NumberStyles.Float, CultureInfo.InvariantCulture, out _)).WithMessage("Select options for number fields must be valid numbers.")
                .When(x => x.SelectOptions != null && x.Type == DataTypes.Number);

            RuleFor(x => x.ReferencedTrackerId)
                .NotEmpty().WithMessage("A reference field needs a tracker to link to.")
                .When(x => x.Type == DataTypes.Reference);

            RuleFor(x => x.IsCalculated)
                .Equal(false).WithMessage("Reference fields cannot be calculated.")
                .When(x => x.Type == DataTypes.Reference);

            RuleFor(x => x.SelectOptions)
                .Empty().WithMessage("Reference fields cannot have suggested options.")
                .When(x => x.Type == DataTypes.Reference);

            RuleFor(x => x.DefaultValue)
                .Empty().WithMessage("A field can have a static default or a constant default, not both.")
                .When(x => x.DefaultValueConstantId != null);

            RuleFor(x => x.DefaultValue)
                .Empty().WithMessage("Reference fields cannot have a default value.")
                .When(x => x.Type == DataTypes.Reference);

            RuleFor(x => x.DefaultValueConstantId)
                .Empty().WithMessage("Reference fields cannot have a default value.")
                .When(x => x.Type == DataTypes.Reference);

            RuleFor(x => x.DefaultValue)
                .Empty().WithMessage("Calculated fields cannot have a default value.")
                .When(x => x.IsCalculated);

            RuleFor(x => x.DefaultValueConstantId)
                .Empty().WithMessage("Calculated fields cannot have a default value.")
                .When(x => x.IsCalculated);

            RuleFor(x => x.DefaultValue)
                .Must(v => double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                .WithMessage("Default value must be a valid number.")
                .When(x => x.Type == DataTypes.Number && !string.IsNullOrEmpty(x.DefaultValue));

            RuleFor(x => x.DefaultValue)
                .Must(v => bool.TryParse(v, out _))
                .WithMessage("Default value must be 'true' or 'false'.")
                .When(x => x.Type == DataTypes.Bool && !string.IsNullOrEmpty(x.DefaultValue));

            RuleFor(x => x.DefaultValue)
                .Must(v => TimeSpan.TryParse(v, CultureInfo.InvariantCulture, out _))
                .WithMessage("Default value must be a valid timespan (e.g. 01:30:00).")
                .When(x => x.Type == DataTypes.TimeSpan && !string.IsNullOrEmpty(x.DefaultValue));

            RuleFor(x => x.DefaultValue)
                .Must(v => DynamicDateTokens.IsValid(v!) || DataFormatters.StringToDateTime(v!) != null)
                .WithMessage("Default value must be a valid date or a relative date token (e.g. today, start_of_month:-1).")
                .When(x => (x.Type == DataTypes.Date || x.Type == DataTypes.DateTime) && !string.IsNullOrEmpty(x.DefaultValue));

            RuleFor(x => x.VisibilityFieldId)
                .Empty().WithMessage("Calculated fields cannot have a visibility condition.")
                .When(x => x.IsCalculated);

            RuleFor(x => x.VisibilityOperator)
                .NotEmpty().WithMessage("Select an operator for the visibility condition.")
                .When(x => !string.IsNullOrEmpty(x.VisibilityFieldId));
        }
    }
}
