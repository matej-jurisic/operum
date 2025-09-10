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
                .NotEmpty().WithMessage("FieldId is required.");
            RuleFor(x => x.Operator)
                .NotEmpty().WithMessage("Operator is required.")
                .Must(OperatorTypes.IsValid)
                .WithMessage($"Operator must be one of: {string.Join(", ", OperatorTypes.All)}");
        }
    }
}
