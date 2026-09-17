using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.Constants.Analytics;
using Operum.Model.DTOs.Analytics.Requests;
using Operum.Model.Enums;
using System.ComponentModel.DataAnnotations;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    public class CreateAndPlaceWidgetSourceDto
    {
        [Required]
        public string TrackerId { get; set; } = string.Empty;

        public List<CreateAnalyticFieldDto> AnalyticFields { get; set; } = [];

        // Fixed view this placement reads through, if any; a filter widget can narrow it further.
        public string? ViewId { get; set; }
        public string? Label { get; set; }
    }

    // Splits into WidgetsService.CreateWidget (definition) followed by PlaceWidget
    // (placement), so the created widget is as reusable as one built from the Library directly.
    public class CreateAndPlaceWidgetDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }

        [Required]
        public string ResultType { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;

        // Line/Bar only.
        public string? Grouping { get; set; }

        // Combined charts only.
        public bool MatchedValuesOnly { get; set; }

        // Goal widgets only: required, a number or hh:mm:ss duration matching the value field's type.
        public string? GoalTarget { get; set; }

        // Goal widgets only. Left unset behaves as HigherIsBetter.
        public string? GoalDirection { get; set; }

        public DashboardItemDisplayMode DisplayMode { get; set; }
        public DashboardItemDisplayMode MobileDisplayMode { get; set; }

        // Line chart widgets only; ignored for every other chart type.
        public bool YAxisFromZero { get; set; } = true;

        // Null/omitted means "Auto". Ignored (not rejected) for a combined/multi-source widget.
        public string? Color { get; set; }

        // Single-source SingleValue/Goal widgets only.
        public bool ShowTrend { get; set; } = true;

        [Required, MinLength(1)]
        public List<CreateAndPlaceWidgetSourceDto> Sources { get; set; } = [];
    }

    public class CreateAndPlaceWidgetSourceDtoValidator : AbstractValidator<CreateAndPlaceWidgetSourceDto>
    {
        public CreateAndPlaceWidgetSourceDtoValidator()
        {
            RuleFor(x => x.TrackerId)
                .NotEmpty().WithMessage(x => Messages.Required("tracker id"));

            RuleFor(x => x.Label)
                .MaximumLength(100).WithMessage("Label cannot exceed 100 characters.")
                .When(x => !string.IsNullOrEmpty(x.Label));

            RuleForEach(x => x.AnalyticFields)
                .SetValidator(new CreateAnalyticFieldDtoValidator());
        }
    }

    public class CreateAndPlaceWidgetDtoValidator : AbstractValidator<CreateAndPlaceWidgetDto>
    {
        public CreateAndPlaceWidgetDtoValidator()
        {
            // Shape check only; DB-dependent validation happens in WidgetsService.
            RuleFor(x => x.ResultType)
                .NotEmpty().WithMessage(x => Messages.Required("result type"))
                .Must(AnalyticTypes.IsValid).WithMessage(x => Messages.Invalid("result type"));

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage(x => Messages.Required("code"))
                .Must(AnalyticCodes.IsValid).WithMessage(x => Messages.Invalid("code"));

            RuleFor(x => x.Grouping)
                .Must(g => string.IsNullOrEmpty(g) || AnalyticGroupings.IsValid(g))
                .WithMessage(x => Messages.Invalid("grouping"));

            RuleFor(x => x.GoalDirection)
                .Must(d => string.IsNullOrEmpty(d) || GoalDirections.IsValid(d))
                .WithMessage(x => Messages.Invalid("goal direction"));

            RuleFor(x => x.Sources)
                .NotEmpty().WithMessage(x => Messages.Required("sources"));

            RuleFor(x => x.DisplayMode)
                .IsInEnum().WithMessage(x => Messages.Invalid("display mode"));
            RuleFor(x => x.MobileDisplayMode)
                .IsInEnum().WithMessage(x => Messages.Invalid("display mode"));

            RuleFor(x => x.Color)
                .MaximumLength(50).WithMessage("Color cannot exceed 50 characters.")
                .When(x => !string.IsNullOrEmpty(x.Color));

            RuleForEach(x => x.Sources)
                .SetValidator(new CreateAndPlaceWidgetSourceDtoValidator());
        }
    }
}
