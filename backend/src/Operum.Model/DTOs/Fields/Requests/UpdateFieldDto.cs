using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.Constants.Fields;

namespace Operum.Model.DTOs.Fields.Requests
{
    public class UpdateFieldDto
    {
        public required string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public required string Type { get; set; } = string.Empty;
        public bool Required { get; set; } = false;
        public List<string>? SelectOptions { get; set; }
    }

    public class UpdateFieldDtoValidator : AbstractValidator<UpdateFieldDto>
    {
        public UpdateFieldDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Field name is required.")
                .MaximumLength(100).WithMessage("Field name cannot exceed 100 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.")
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.Type)
                .NotEmpty().WithMessage((x) => Messages.Required("field type"))
                .Must(DataTypes.IsValid).WithMessage((x) => Messages.Invalid("field tye"));

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
