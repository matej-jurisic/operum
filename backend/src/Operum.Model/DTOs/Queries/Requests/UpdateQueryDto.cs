using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Queries.Requests
{
    public class UpdateQueryDto
    {
        public required string Kind { get; set; } = string.Empty;
        public required string FieldId { get; set; } = string.Empty;

        public string? Operator { get; set; }
        public string? Value { get; set; }

        public bool Descending { get; set; }
    }

    public class UpdateQueryDtoValidator : AbstractValidator<UpdateQueryDto>
    {
        public UpdateQueryDtoValidator()
        {
            RuleFor(x => x.Kind)
                .NotEmpty().WithMessage((x) => Messages.Required("kind"))
                .Must(QueryKinds.IsValid).WithMessage((x) => Messages.Invalid("kind"));

            RuleFor(x => x.FieldId)
                .NotEmpty().WithMessage((x) => Messages.Required("field id"));

            RuleFor(x => x.Operator)
                .NotEmpty().WithMessage((x) => Messages.Required("operator"))
                .Must(op => op != null && OperatorTypes.IsValid(op)).WithMessage((x) => Messages.Invalid("operator"))
                .When(x => x.Kind == QueryKinds.Filter);
        }
    }
}
