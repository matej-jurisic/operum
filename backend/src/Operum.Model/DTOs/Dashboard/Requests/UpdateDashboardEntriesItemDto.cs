using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.Enums;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    // Edits an Entries widget in place: only which columns it shows, and whether it collapses
    // to a button on each grid. This carries no TrackerId — changing which tracker the table
    // reads from would make it a different widget, which is what adding a new one is for. How
    // it's filtered comes only from the view selector widgets it's linked to.
    public class UpdateDashboardEntriesItemDto
    {
        // The tracker fields to show as columns, in order. Empty shows every field.
        public List<string> ColumnFieldIds { get; set; } = [];

        // How the widget draws on each of the board's two grids — inline, as a button that
        // opens the table in a modal, or dropped from that grid entirely.
        public DashboardItemDisplayMode DisplayMode { get; set; }
        public DashboardItemDisplayMode MobileDisplayMode { get; set; }
    }

    public class UpdateDashboardEntriesItemDtoValidator : AbstractValidator<UpdateDashboardEntriesItemDto>
    {
        public UpdateDashboardEntriesItemDtoValidator()
        {
            RuleFor(x => x.DisplayMode)
                .IsInEnum().WithMessage(x => Messages.Invalid("display mode"));
            RuleFor(x => x.MobileDisplayMode)
                .IsInEnum().WithMessage(x => Messages.Invalid("display mode"));
        }
    }
}
