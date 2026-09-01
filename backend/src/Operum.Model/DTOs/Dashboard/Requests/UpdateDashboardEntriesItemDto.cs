using FluentValidation;

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
