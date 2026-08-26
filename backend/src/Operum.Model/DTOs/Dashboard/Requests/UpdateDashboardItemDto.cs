using FluentValidation;
using Operum.Model.Constants;
using System.ComponentModel.DataAnnotations;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    // One source of an analytic widget as the board is allowed to change it afterwards:
    // what the series is called, and which view narrows the entries it reads.
    public class UpdateDashboardItemSourceDto
    {
        [Required]
        public string SourceId { get; set; } = string.Empty;

        // Cleared when left blank, so the widget falls back to the definition's own label
        // the way an item added without a label does.
        public string? Label { get; set; }

        // At most one of these, exactly as when the source was created — see
        // DashboardItemSourceRequestDto.
        public string? ViewId { get; set; }
        public string? LinkedViewWidgetId { get; set; }
    }

    // Edits an analytic widget in place. Only the parts that belong to the board are
    // editable: the item's result type, code and field mapping are the definition it was
    // built from, and changing those would silently turn the widget into a different chart
    // rather than the one the user put there — that is what adding a new widget is for.
    public class UpdateDashboardItemDto
    {
        // Whether the widget draws as a small button that opens the chart in a modal
        // instead of inline, independently on each of the board's two grids.
        public bool Expandable { get; set; }
        public bool MobileExpandable { get; set; }

        // Every source of the item, named once each: the payload is the whole widget, so a
        // label or a view left out means "cleared" rather than "unchanged", the same way an
        // entry's payload is the whole entry.
        [Required, MinLength(1)]
        public List<UpdateDashboardItemSourceDto> Sources { get; set; } = [];
    }

    public class UpdateDashboardItemSourceDtoValidator : AbstractValidator<UpdateDashboardItemSourceDto>
    {
        public UpdateDashboardItemSourceDtoValidator()
        {
            RuleFor(x => x.SourceId)
                .NotEmpty().WithMessage(x => Messages.Required("source id"));

            RuleFor(x => x.Label)
                .MaximumLength(100).WithMessage("Label cannot exceed 100 characters.")
                .When(x => !string.IsNullOrEmpty(x.Label));

            RuleFor(x => x)
                .Must(x => string.IsNullOrEmpty(x.ViewId) || string.IsNullOrEmpty(x.LinkedViewWidgetId))
                .WithMessage("A source cannot both filter by a fixed view and follow a view widget.");
        }
    }

    public class UpdateDashboardItemDtoValidator : AbstractValidator<UpdateDashboardItemDto>
    {
        public UpdateDashboardItemDtoValidator()
        {
            // Shape only. Whether the ids name this item's own sources, and whether a view
            // or a view widget actually goes with the source's tracker, is settled in
            // DashboardService.
            RuleFor(x => x.Sources)
                .NotEmpty().WithMessage(x => Messages.Required("sources"));

            RuleForEach(x => x.Sources)
                .SetValidator(new UpdateDashboardItemSourceDtoValidator());
        }
    }
}
