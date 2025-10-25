using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Views.Requests
{
    public class CreateViewFilterDto
    {
        public required string FieldId { get; set; } = string.Empty;
        public required string Operator { get; set; } = string.Empty;
        public string? Value { get; set; }
    }

    public class CreateViewFilterDtoValidator : AbstractValidator<CreateViewFilterDto>
    {
        public CreateViewFilterDtoValidator()
        {
            RuleFor(x => x.FieldId)
                .NotEmpty().WithMessage((x) => Messages.Required("field id"));
            RuleFor(x => x.Operator)
                .NotEmpty().WithMessage((x) => Messages.Required("operator"))
                .Must(OperatorTypes.IsValid).WithMessage((x) => Messages.Invalid("operator"));
        }
    }
}
