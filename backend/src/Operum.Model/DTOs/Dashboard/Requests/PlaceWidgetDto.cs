using FluentValidation;
using Operum.Model.Constants;
using System.ComponentModel.DataAnnotations;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    // One WidgetSource's placement-only settings: how this board filters and labels it.
    // Everything else about the source (which tracker, which fields) is the shared
    // definition and comes from the Widget itself.
    public class PlaceWidgetSourceOverrideDto
    {
        [Required]
        public string WidgetSourceId { get; set; } = string.Empty;

        public string? Label { get; set; }
        public string? ViewId { get; set; }
    }

    // Places an existing Widget Library chart onto this dashboard by reference: unlike the
    // old AddDashboardItemFromAnalytic, nothing is copied. Editing the widget afterwards --
    // in the Library, or from any other dashboard placing it -- changes what this placement
    // draws too. Only what's specific to this board (the filter and layout) lives here.
    public class PlaceWidgetDto
    {
        [Required]
        public string WidgetId { get; set; } = string.Empty;

        // Whether the widget draws as a small button that opens the chart in a modal
        // instead of inline, independently on each of the board's two grids.
        public bool Expandable { get; set; }
        public bool MobileExpandable { get; set; }

        // Line chart widgets only: whether the y-axis is anchored at zero (the default) or
        // fitted to the data's own range. Ignored for every other chart type.
        public bool YAxisFromZero { get; set; } = true;

        // A WidgetSource not named here is placed with no label or view override: the
        // widget's own display name, unfiltered.
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

            RuleForEach(x => x.SourceOverrides)
                .SetValidator(new PlaceWidgetSourceOverrideDtoValidator());
        }
    }
}
