using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.Enums;
using System.ComponentModel.DataAnnotations;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    public class PlaceWidgetSourceOverrideDto
    {
        [Required]
        public string WidgetSourceId { get; set; } = string.Empty;

        public string? Label { get; set; }
        public string? ViewId { get; set; }
    }

    // Places by reference; nothing is copied, so editing the widget elsewhere changes what
    // this placement draws too.
    public class PlaceWidgetDto
    {
        [Required]
        public string WidgetId { get; set; } = string.Empty;

        public DashboardItemDisplayMode DisplayMode { get; set; }
        public DashboardItemDisplayMode MobileDisplayMode { get; set; }

        // Line chart widgets only; ignored for every other chart type.
        public bool YAxisFromZero { get; set; } = true;

        // Null/omitted means "Auto". Ignored (not rejected) for a combined/multi-source widget.
        public string? Color { get; set; }

        // Single-source SingleValue/Goal widgets only.
        public bool ShowTrend { get; set; } = true;

        // A WidgetSource not named here uses the widget's own display name, unfiltered.
        public List<PlaceWidgetSourceOverrideDto> SourceOverrides { get; set; } = [];
    }

    public class PlaceWidgetSourceOverrideDtoValidator : AbstractValidator<PlaceWidgetSourceOverrideDto>
    {
        public PlaceWidgetSourceOverrideDtoValidator()
        {
            RuleFor(x => x.WidgetSourceId)
                .NotEmpty().WithMessage(x => Messages.Required("widget source id"));

            RuleFor(x => x.Label)
                .MaximumLength(100).WithMessage("Label cannot exceed 100 characters.")
                .When(x => !string.IsNullOrEmpty(x.Label));
        }
    }

    public class PlaceWidgetDtoValidator : AbstractValidator<PlaceWidgetDto>
    {
        public PlaceWidgetDtoValidator()
        {
            RuleFor(x => x.WidgetId)
                .NotEmpty().WithMessage(x => Messages.Required("widget id"));

            RuleFor(x => x.DisplayMode)
                .IsInEnum().WithMessage(x => Messages.Invalid("display mode"));
            RuleFor(x => x.MobileDisplayMode)
                .IsInEnum().WithMessage(x => Messages.Invalid("display mode"));

            RuleFor(x => x.Color)
                .MaximumLength(50).WithMessage("Color cannot exceed 50 characters.")
                .When(x => !string.IsNullOrEmpty(x.Color));

            RuleForEach(x => x.SourceOverrides)
                .SetValidator(new PlaceWidgetSourceOverrideDtoValidator());
        }
    }
}
