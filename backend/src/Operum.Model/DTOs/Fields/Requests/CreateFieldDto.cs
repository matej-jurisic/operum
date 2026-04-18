using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.Constants.Fields;

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
                .Must(opt => double.TryParse(opt, out _)).WithMessage("Select options for number fields must be valid numbers.")
                .When(x => x.SelectOptions != null && x.Type == DataTypes.Number);
        }
    }
}
