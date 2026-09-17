using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.Enums;
using System.ComponentModel.DataAnnotations;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    // The tracker is fixed on the EntriesWidget itself; only columns and layout are this
    // placement's own. See PlaceWidgetDto for the equivalent on a chart.
    public class PlaceEntriesWidgetDto
    {
        [Required]
        public string EntriesWidgetId { get; set; } = string.Empty;

        // Empty shows every field.
        public List<string> ColumnFieldIds { get; set; } = [];

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
