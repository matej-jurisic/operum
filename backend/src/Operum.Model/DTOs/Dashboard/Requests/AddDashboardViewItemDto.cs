using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    // Adds a widget that is a dropdown over one tracker's views, rather than a chart. Other
    // widgets' sources can point their LinkedViewWidgetId at it afterwards to follow whatever
    // it's currently set to.
    public class AddDashboardViewItemDto
    {
        public string TrackerId { get; set; } = string.Empty;

        // The dropdown's starting selection. Left null to start on "All entries".
        public string? ViewId { get; set; }

        // Dashboard item ids of Analytic/Entries widgets already on the board that should
        // follow this selector from the moment it's added, so the user doesn't have to open
        // each one afterwards to link it. Every source of the item that reads from TrackerId
        // is pointed at the new widget; sources on other trackers are left alone. Ids that
        // aren't a linkable widget for this tracker are rejected — see DashboardService.
        public List<string> LinkedItemIds { get; set; } = [];
    }

    public class AddDashboardViewItemDtoValidator : AbstractValidator<AddDashboardViewItemDto>
    {
        public AddDashboardViewItemDtoValidator()
        {
            RuleFor(x => x.TrackerId)
                .NotEmpty().WithMessage(x => Messages.Required("tracker id"));
        }
    }
}
