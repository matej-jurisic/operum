using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.Constants.Analytics;

namespace Operum.Model.DTOs.Analytics.Requests
{
    // A blank value means "is empty"/"has a value" for the two equality operators; otherwise
    // the clause is unset.
    public class EvaluateFilterClauseDto
    {
        public string FieldId { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public string? Value { get; set; }
    }

    public class EvaluateSourceDto
    {
        public string TrackerId { get; set; } = string.Empty;
        public List<CreateAnalyticFieldDto> Fields { get; set; } = [];
        public string? ViewId { get; set; }
        public List<EvaluateFilterClauseDto> Filters { get; set; } = [];
    }

    public class EvaluateWidgetDto
    {
        public string ResultType { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;

        // Line/Bar only. A legacy fused code with no grouping is resolved server-side
        // (LegacyLineBarCodes).
        public string? Grouping { get; set; }

        // Combined charts only; ignored for a single source or a paired correlation.
        public bool MatchedValuesOnly { get; set; }

        public List<EvaluateSourceDto> Sources { get; set; } = [];
    }

    public class EvaluateSourceDtoValidator : AbstractValidator<EvaluateSourceDto>
    {
        public EvaluateSourceDtoValidator()
        {
            RuleFor(x => x.TrackerId)
                .NotEmpty().WithMessage(x => Messages.Required("tracker id"));

            RuleForEach(x => x.Fields)
                .SetValidator(new CreateAnalyticFieldDtoValidator());
        }
    }

    public class EvaluateWidgetDtoValidator : AbstractValidator<EvaluateWidgetDto>
    {
        public EvaluateWidgetDtoValidator()
        {
            // Shape check only; DB-dependent validation happens in AnalyticsService.
            RuleFor(x => x.ResultType)
                .NotEmpty().WithMessage(x => Messages.Required("result type"))
                .Must(AnalyticTypes.IsValid).WithMessage(x => Messages.Invalid("result type"));

            // A legacy fused code passes here and is rewritten to (Grouping, Code) in AnalyticsService.
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage(x => Messages.Required("code"))
                .Must(c => AnalyticCodes.IsValid(c) || LegacyLineBarCodes.Map.ContainsKey(c))
                .WithMessage(x => Messages.Invalid("code"));

            RuleFor(x => x.Grouping)
                .Must(g => string.IsNullOrEmpty(g) || AnalyticGroupings.IsValid(g))
                .WithMessage(x => Messages.Invalid("grouping"));

            RuleFor(x => x.Sources)
                .NotEmpty().WithMessage(x => Messages.Required("sources"))
                .Must(s => s.Count <= DataLimits.MaxDashboardItemSourceCount)
                .WithMessage(x => Messages.MaxNumberReached("sources", DataLimits.MaxDashboardItemSourceCount));

            RuleForEach(x => x.Sources)
                .SetValidator(new EvaluateSourceDtoValidator());
        }
    }
}
