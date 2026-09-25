using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.Enums;
using System.ComponentModel.DataAnnotations;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    // Single-round-trip convenience EntriesWidgetForm relies on. See CreateAndPlaceWidgetDto
    // for the equivalent on a chart.
    public class CreateAndPlaceEntriesWidgetDto
    {
        [Required]
        public string TrackerId { get; set; } = string.Empty;
        public string? Name { get; set; }

        // Empty shows every field.
        public List<string> ColumnFieldIds { get; set; } = [];

        public string? ViewId { get; set; }

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
