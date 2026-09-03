using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.Constants.Analytics;

namespace Operum.Model.DTOs.Analytics.Requests
{
    // One inline, field-bound filter clause for an ad hoc evaluation. Shaped like a view's
    // resolved filter (ViewQueryBuilder.ResolvedClause) minus the field type, which is read
    // from the tracker server-side. A blank value only means "is empty" / "has a value" for
    // the two equality operators; for anything else it means the clause is unset.
    public class EvaluateFilterClauseDto
    {
        public string FieldId { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public string? Value { get; set; }
    }

    // A chart definition evaluated once, against live data, without being saved anywhere --
    // the Explore page's request. Single source only: the tracker, the calculation, the
    // purpose -> field mapping, an optional saved view for the base filter/sort, and any
    // number of inline clauses ANDed on top.
    public class EvaluateWidgetDto
    {
        public string ResultType { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string TrackerId { get; set; } = string.Empty;
        public List<CreateAnalyticFieldDto> Fields { get; set; } = [];
        public string? ViewId { get; set; }
        public List<EvaluateFilterClauseDto> Filters { get; set; } = [];
    }

    public class EvaluateWidgetDtoValidator : AbstractValidator<EvaluateWidgetDto>
    {
        public EvaluateWidgetDtoValidator()
        {
            // Shape check only. Whether the code goes with the result type, and whether the
            // fields exist, belong to the tracker and carry compatible data types, is
            // settled in AnalyticsService, which is the only place with database access.
            RuleFor(x => x.ResultType)
                .NotEmpty().WithMessage(x => Messages.Required("result type"))
                .Must(AnalyticTypes.IsValid).WithMessage(x => Messages.Invalid("result type"));

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage(x => Messages.Required("code"))
                .Must(AnalyticCodes.IsValid).WithMessage(x => Messages.Invalid("code"));

            RuleFor(x => x.TrackerId)
                .NotEmpty().WithMessage(x => Messages.Required("tracker id"));

            RuleForEach(x => x.Fields)
                .SetValidator(new CreateAnalyticFieldDtoValidator());
        }
    }
}
