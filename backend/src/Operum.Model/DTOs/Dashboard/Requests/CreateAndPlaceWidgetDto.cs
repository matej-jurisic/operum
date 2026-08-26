using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.Constants.Analytics;
using Operum.Model.DTOs.Analytics.Requests;
using System.ComponentModel.DataAnnotations;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    public class CreateAndPlaceWidgetSourceDto
    {
        [Required]
        public string TrackerId { get; set; } = string.Empty;

        // Which of the tracker's fields fill the purposes required by the widget's
        // ResultType + Code. The definition itself is shared by every source, so a source
        // only ever supplies the tracker-specific half of it.
        public List<CreateAnalyticFieldDto> AnalyticFields { get; set; } = [];

        // At most one of these narrows this placement's entries: ViewId fixes it,
        // LinkedViewWidgetId instead follows a DashboardWidgetTypes.View item already on the
        // board, so the filter changes live with its dropdown.
        public string? ViewId { get; set; }
        public string? LinkedViewWidgetId { get; set; }
        public string? Label { get; set; }
    }

    // Defines a new Widget Library chart and places it on this dashboard in one call --
    // the single-round-trip convenience CustomAnalyticForm relies on. Splits into
    // WidgetsService.CreateWidget (the definition: ResultType/Code/AnalyticFields) followed
    // by PlaceWidget (the placement: Label/ViewId/LinkedViewWidgetId/layout), so the widget
    // this creates is exactly as reusable afterwards as one built from the Library directly.
    public class CreateAndPlaceWidgetDto
    {
        // Optional: left unset, the widget falls back to its definition's own label (e.g.
        // "Sum") the way a tracker analytic used to.
        public string? Name { get; set; }
        public string? Description { get; set; }

        // One definition for the whole widget: every source is calculated the same way, so
        // the series of a multi-tracker chart always share an axis semantics.
        [Required]
        public string ResultType { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;

        // Combined charts only: keep just the x-axis values every source has a point for,
        // so the series line up over the same range. A single-source item ignores it.
        public bool MatchedValuesOnly { get; set; }

        // Whether the widget draws as a small button that opens the chart in a modal
        // instead of inline, independently on each of the board's two grids.
        public bool Expandable { get; set; }
        public bool MobileExpandable { get; set; }

        [Required, MinLength(1)]
        public List<CreateAndPlaceWidgetSourceDto> Sources { get; set; } = [];
    }

    public class CreateAndPlaceWidgetSourceDtoValidator : AbstractValidator<CreateAndPlaceWidgetSourceDto>
    {
        public CreateAndPlaceWidgetSourceDtoValidator()
        {
            RuleFor(x => x.TrackerId)
                .NotEmpty().WithMessage(x => Messages.Required("tracker id"));

            RuleFor(x => x.Label)
                .MaximumLength(100).WithMessage("Label cannot exceed 100 characters.")
                .When(x => !string.IsNullOrEmpty(x.Label));

            RuleFor(x => x)
                .Must(x => string.IsNullOrEmpty(x.ViewId) || string.IsNullOrEmpty(x.LinkedViewWidgetId))
                .WithMessage("A source cannot both filter by a fixed view and follow a view widget.");

            RuleForEach(x => x.AnalyticFields)
                .SetValidator(new CreateAnalyticFieldDtoValidator());
        }
    }

    public class CreateAndPlaceWidgetDtoValidator : AbstractValidator<CreateAndPlaceWidgetDto>
    {
        public CreateAndPlaceWidgetDtoValidator()
        {
            // Shape check only. Whether the code goes with the result type, and whether the
            // fields exist, belong to their tracker and carry compatible data types, is
            // settled in WidgetsService, which is the only place with database access.
            RuleFor(x => x.ResultType)
                .NotEmpty().WithMessage(x => Messages.Required("result type"))
                .Must(AnalyticTypes.IsValid).WithMessage(x => Messages.Invalid("result type"));

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage(x => Messages.Required("code"))
                .Must(AnalyticCodes.IsValid).WithMessage(x => Messages.Invalid("code"));

            RuleFor(x => x.Sources)
                .NotEmpty().WithMessage(x => Messages.Required("sources"));

            RuleForEach(x => x.Sources)
                .SetValidator(new CreateAndPlaceWidgetSourceDtoValidator());
        }
    }
}
