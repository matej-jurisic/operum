using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Views.Requests
{
    public class CreateViewFilterDto
    {
        public string FieldId { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public string? Value { get; set; }
    }

    public class CreateViewFilterDtoValidator : AbstractValidator<CreateViewFilterDto>
    {
        public CreateViewFilterDtoValidator()
        {
            RuleFor(x => x.FieldId)
                .NotEmpty().WithMessage(Messages.Required("field id"));
            RuleFor(x => x.Operator)
                .NotEmpty().WithMessage(Messages.Required("operator"))
                .Must(OperatorTypes.IsValid).WithMessage(Messages.Invalid("operator"));
        }
    }
}
