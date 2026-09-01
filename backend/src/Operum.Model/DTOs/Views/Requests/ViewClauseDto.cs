using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Views.Requests
{
    // One entry in a View's ordered clause list: a filter or a sort, expressed against one
    // of the tracker's fields. The clause itself is field-agnostic and pooled on save (see
    // QueryPool); FieldId is the binding. The data type is taken from the field server-side,
    // so it is not sent here.
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
