using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    // Edits a DashboardWidgetTypes.View item in place: its starting/current selection, and
    // the full set of widgets on the board that follow it. Like UpdateDashboardItemDto the
    // payload stands for the whole thing — a widget left out of LinkedItemIds is unlinked
    // (only from this selector; a fixed view or a link to a different selector is left as
    // it was), not "unchanged".
    public class UpdateDashboardViewItemDto
    {
        // Null clears the filter back to "All entries".
        public string? ViewId { get; set; }

        // Every Analytic/Entries item that should follow this selector, named once each.
        // Ids that aren't a linkable widget for this selector's tracker are rejected — see
        // DashboardService.
        public List<string> LinkedItemIds { get; set; } = [];
    }

    public class UpdateDashboardViewItemDtoValidator : AbstractValidator<UpdateDashboardViewItemDto>
    {
        public UpdateDashboardViewItemDtoValidator()
        {
            // Shape only. Whether the ids name Analytic/Entries widgets on this board built
            // for the selector's tracker is settled in DashboardService.
            RuleForEach(x => x.LinkedItemIds)
                .NotEmpty().WithMessage(x => Messages.Required("linked item id"));
        }
    }
}
