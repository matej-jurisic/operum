using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    // Adds a widget that is a read-only table of one tracker's entries, rather than a
    // chart. At most one of ViewId/LinkedViewWidgetId narrows what it shows: ViewId fixes
    // it, LinkedViewWidgetId instead follows a DashboardWidgetTypes.View item already on the
    // board, so the table (and the columns it shows) changes live with its dropdown.
    public class AddDashboardEntriesItemDto
    {
        public string TrackerId { get; set; } = string.Empty;
        public string? ViewId { get; set; }
        public string? LinkedViewWidgetId { get; set; }
    }

    public class AddDashboardEntriesItemDtoValidator : AbstractValidator<AddDashboardEntriesItemDto>
    {
        public AddDashboardEntriesItemDtoValidator()
        {
            RuleFor(x => x.TrackerId)
                .NotEmpty().WithMessage(x => Messages.Required("tracker id"));

            RuleFor(x => x)
                .Must(x => string.IsNullOrEmpty(x.ViewId) || string.IsNullOrEmpty(x.LinkedViewWidgetId))
                .WithMessage("An entries widget cannot both filter by a fixed view and follow a view widget.");
        }
    }
}
