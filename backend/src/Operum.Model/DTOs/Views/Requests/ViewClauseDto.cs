using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Views.Requests
{
    // Field-agnostic, pooled on save (see QueryPool). Data type comes from the field server-side.
    public class ViewClauseDto
    {
        public string Kind { get; set; } = QueryKinds.Filter;
        public string FieldId { get; set; } = string.Empty;

        // Filters only. Operator is required for a filter; a null Value means "has no value".
        public string? Operator { get; set; }
        public string? Value { get; set; }

        // Sorts only.
        public bool Descending { get; set; }
    }

    public class ViewClauseDtoValidator : AbstractValidator<ViewClauseDto>
    {
        public ViewClauseDtoValidator()
        {
            RuleFor(x => x.Kind)
                .NotEmpty().WithMessage(x => Messages.Required("kind"))
                .Must(QueryKinds.IsValid).WithMessage(x => Messages.Invalid("kind"));

            RuleFor(x => x.FieldId)
                .NotEmpty().WithMessage(x => Messages.Required("field id"));

            RuleFor(x => x.Operator)
                .NotEmpty().WithMessage(x => Messages.Required("operator"))
                .Must(op => op != null && OperatorTypes.IsValid(op)).WithMessage(x => Messages.Invalid("operator"))
                .When(x => x.Kind == QueryKinds.Filter);
        }
    }
}
