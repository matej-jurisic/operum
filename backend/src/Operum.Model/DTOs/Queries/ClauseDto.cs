using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.Constants.Fields;

namespace Operum.Model.DTOs.Queries
{
    // A field-agnostic clause as the client sends it: one filter or one sort over a data
    // type. The concrete field is bound elsewhere (ViewClauseDto.FieldId for a tracker view,
    // a view selector's per-widget map on a dashboard). Shared by the View editor and the
    // DashboardView editor; deduplicated into a pooled Query on save (see QueryPool).
    public class ClauseDto
    {
        public string Kind { get; set; } = QueryKinds.Filter;
        public string DataType { get; set; } = string.Empty;

        // Filters only. A null Value means "has no value".
        public string? Operator { get; set; }
        public string? Value { get; set; }

        // Sorts only.
        public bool Descending { get; set; }
    }

    public class ClauseDtoValidator : AbstractValidator<ClauseDto>
    {
        public ClauseDtoValidator()
        {
            RuleFor(x => x.Kind)
                .NotEmpty().WithMessage(x => Messages.Required("kind"))
                .Must(QueryKinds.IsValid).WithMessage(x => Messages.Invalid("kind"));

            RuleFor(x => x.DataType)
                .NotEmpty().WithMessage(x => Messages.Required("data type"))
                .Must(DataTypes.IsValid).WithMessage(x => Messages.Invalid("data type"));

            RuleFor(x => x.Operator)
                .NotEmpty().WithMessage(x => Messages.Required("operator"))
                .Must(op => op != null && OperatorTypes.IsValid(op)).WithMessage(x => Messages.Invalid("operator"))
                .When(x => x.Kind == QueryKinds.Filter);
        }
    }
}
