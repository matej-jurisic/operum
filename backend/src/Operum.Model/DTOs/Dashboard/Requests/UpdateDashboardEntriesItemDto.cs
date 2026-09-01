using FluentValidation;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    // Edits an Entries widget in place: only how it's filtered, and whether it collapses to
    // a button on each grid. Unlike AddDashboardEntriesItemDto this carries no TrackerId —
    // changing which tracker the table reads from would make it a different widget, which is
    // what adding a new one is for.
    public class UpdateDashboardEntriesItemDto
    {
        public string? ViewId { get; set; }

        // Whether the widget draws as a small button that opens the table in a modal
        // instead of inline, independently on each of the board's two grids.
        public bool Expandable { get; set; }
        public bool MobileExpandable { get; set; }
    }

    public class UpdateDashboardEntriesItemDtoValidator : AbstractValidator<UpdateDashboardEntriesItemDto>
    {
        public UpdateDashboardEntriesItemDtoValidator()
        {
        }
    }
}
