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
                .Must(DataTypes.IsValid).WithMessage((x) => Messages.Invalid("field tye"));

            RuleForEach(x => x.SelectOptions)
                .NotEmpty().WithMessage("Select option cannot be empty.")
                .MaximumLength(1000).WithMessage("Select option cannot exceed 1000 characters.")
                .When(x => x.SelectOptions != null);
        }
    }
}
