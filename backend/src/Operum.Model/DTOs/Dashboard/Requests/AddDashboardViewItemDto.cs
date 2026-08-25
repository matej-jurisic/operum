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
