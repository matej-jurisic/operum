using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.Enums;
using System.ComponentModel.DataAnnotations;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    // Defines a new Widget Library Entries table and places it on this dashboard in one
    // call -- the single-round-trip convenience EntriesWidgetForm relies on. See
    // CreateAndPlaceWidgetDto for the equivalent on a chart.
    public class CreateAndPlaceEntriesWidgetDto
    {
        [Required]
        public string TrackerId { get; set; } = string.Empty;
        public string? Name { get; set; }

        // The tracker fields to show as columns, in order. Empty shows every field.
        public List<string> ColumnFieldIds { get; set; } = [];

        // How the table draws on each of the board's two grids — inline, as a button that
        // opens it in a modal, or dropped from that grid entirely.
        public DashboardItemDisplayMode DisplayMode { get; set; }
        public DashboardItemDisplayMode MobileDisplayMode { get; set; }
    }

    public class CreateAndPlaceEntriesWidgetDtoValidator : AbstractValidator<CreateAndPlaceEntriesWidgetDto>
    {
        public CreateAndPlaceEntriesWidgetDtoValidator()
        {
            RuleFor(x => x.TrackerId)
                .NotEmpty().WithMessage(x => Messages.Required("tracker id"));

            RuleFor(x => x.DisplayMode)
                .IsInEnum().WithMessage(x => Messages.Invalid("display mode"));
            RuleFor(x => x.MobileDisplayMode)
                .IsInEnum().WithMessage(x => Messages.Invalid("display mode"));
        }
    }
}
