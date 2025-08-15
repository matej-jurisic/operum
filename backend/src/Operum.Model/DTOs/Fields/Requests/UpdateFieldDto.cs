using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Fields.Requests
{
    public class UpdateFieldDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Type { get; set; } = string.Empty;
        public bool Required { get; set; } = false;
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
                .NotEmpty().WithMessage("Field type is required.")
                .Must(type => DataTypes.IsValid(type.ToLower()))
                .WithMessage($"Field type must be one of: {string.Join(", ", DataTypes.All)}");
        }
    }
}
