using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.Constants.Fields;

namespace Operum.Model.DTOs.Fields.Requests
{
    public class CreateFieldDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Type { get; set; } = string.Empty;
        public bool Required { get; set; } = false;
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
        }
    }
}
