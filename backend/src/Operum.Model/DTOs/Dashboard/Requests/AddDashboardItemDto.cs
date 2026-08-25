using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.Constants.Analytics;
using Operum.Model.DTOs.Analytics.Requests;
using System.ComponentModel.DataAnnotations;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    public class DashboardItemSourceRequestDto
    {
        [Required]
        public string TrackerId { get; set; } = string.Empty;

        // Which of the tracker's fields fill the purposes required by the item's
        // ResultType + Code. The definition itself is shared by every source, so a source
        // only ever supplies the tracker-specific half of it.
        public List<CreateAnalyticFieldDto> AnalyticFields { get; set; } = [];

        public string? ViewId { get; set; }
        public string? Label { get; set; }
    }

    public class AddDashboardItemDto
    {
        // One definition for the whole item: every source is calculated the same way, so
        // the series of a multi-tracker chart always share an axis semantics.
        [Required]
        public string ResultType { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;

        // Combined charts only: keep just the x-axis values every source has a point for,
        // so the series line up over the same range. A single-source item ignores it.
        public bool MatchedValuesOnly { get; set; }

        [Required, MinLength(1)]
        public List<DashboardItemSourceRequestDto> Sources { get; set; } = [];
    }

    public class DashboardItemSourceRequestDtoValidator : AbstractValidator<DashboardItemSourceRequestDto>
    {
        public DashboardItemSourceRequestDtoValidator()
        {
            RuleFor(x => x.TrackerId)
                .NotEmpty().WithMessage(x => Messages.Required("tracker id"));

            RuleFor(x => x.Label)
                .MaximumLength(100).WithMessage("Label cannot exceed 100 characters.")
                .When(x => !string.IsNullOrEmpty(x.Label));

            RuleForEach(x => x.AnalyticFields)
                .SetValidator(new CreateAnalyticFieldDtoValidator());
        }
    }

    public class AddDashboardItemDtoValidator : AbstractValidator<AddDashboardItemDto>
    {
        public AddDashboardItemDtoValidator()
        {
            // Shape check only. Whether the code goes with the result type, and whether the
            // fields exist, belong to their tracker and carry compatible data types, is
            // settled in DashboardService, which is the only place with database access.
            RuleFor(x => x.ResultType)
                .NotEmpty().WithMessage(x => Messages.Required("result type"))
                .Must(AnalyticTypes.IsValid).WithMessage(x => Messages.Invalid("result type"));

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage(x => Messages.Required("code"))
                .Must(AnalyticCodes.IsValid).WithMessage(x => Messages.Invalid("code"));

            RuleFor(x => x.Sources)
                .NotEmpty().WithMessage(x => Messages.Required("sources"));

            RuleForEach(x => x.Sources)
                .SetValidator(new DashboardItemSourceRequestDtoValidator());
        }
    }
}
