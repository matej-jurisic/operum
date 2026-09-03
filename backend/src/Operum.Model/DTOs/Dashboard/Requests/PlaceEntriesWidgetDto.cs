using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.Enums;
using System.ComponentModel.DataAnnotations;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    // Places an existing Widget Library Entries table onto this dashboard by reference --
    // see PlaceWidgetDto for the equivalent on a chart. The tracker it reads from is fixed
    // on the EntriesWidget itself; only the columns and layout are this placement's own.
    public class PlaceEntriesWidgetDto
    {
        [Required]
        public string EntriesWidgetId { get; set; } = string.Empty;

        // The tracker fields to show as columns, in order. Empty shows every field.
        public List<string> ColumnFieldIds { get; set; } = [];

        // How the table draws on each of the board's two grids — inline, as a button that
        // opens it in a modal, or dropped from that grid entirely.
        public DashboardItemDisplayMode DisplayMode { get; set; }
        public DashboardItemDisplayMode MobileDisplayMode { get; set; }
    }

    public class PlaceEntriesWidgetDtoValidator : AbstractValidator<PlaceEntriesWidgetDto>
    {
        public PlaceEntriesWidgetDtoValidator()
        {
            RuleFor(x => x.EntriesWidgetId)
                .NotEmpty().WithMessage(x => Messages.Required("entries widget id"));

            RuleFor(x => x.DisplayMode)
                .IsInEnum().WithMessage(x => Messages.Invalid("display mode"));
            RuleFor(x => x.MobileDisplayMode)
                .IsInEnum().WithMessage(x => Messages.Invalid("display mode"));
        }
    }
}
