using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.Constants.Analytics;
using Operum.Model.Enums;
using System.ComponentModel.DataAnnotations;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    public class UpdateDashboardItemSourceDto
    {
        [Required]
        public string SourceId { get; set; } = string.Empty;

        // Cleared when left blank.
        public string? Label { get; set; }

        public string? ViewId { get; set; }
    }

    // Only board-owned parts are editable; result type, code and field mapping are fixed
    // (changing those means adding a new widget instead).
    public class UpdateDashboardItemDto
    {
        // Lives on the shared Widget, not the placement -- editing it here touches every
        // board this widget is placed on. Cleared when left blank, same as at creation.
        public string? Name { get; set; }

        public DashboardItemDisplayMode DisplayMode { get; set; }
        public DashboardItemDisplayMode MobileDisplayMode { get; set; }

        // Line chart widgets only; ignored for every other chart type.
        public bool YAxisFromZero { get; set; } = true;

        // Combined Line/Bar charts only; ignored otherwise. Also shared-Widget-scoped.
        public bool MatchedValuesOnly { get; set; }

        // Goal widgets only. Left blank, the existing target is kept (a Goal always has one).
        public string? GoalTarget { get; set; }

        // Goal widgets only. Left blank, the existing direction is kept.
        public string? GoalDirection { get; set; }

        // Goal widgets only. Whole-list replace: empty clears them. Each Conditions key must
        // be a filter clause this placement currently follows; DashboardService checks that.
        public List<GoalConditionalTargetDto> GoalConditionalTargets { get; set; } = [];

        // Null clears the override. Ignored (not rejected) for a combined/multi-source widget.
        public string? Color { get; set; }

        // Single-source SingleValue/Goal widgets only.
        public bool ShowTrend { get; set; } = true;

        // Calendar widgets only: a CalendarStartMonths value, null for automatic.
        public string? CalendarStartMonth { get; set; }

        // Whole-list replace: a source's Label/ViewId left out means "cleared", not "unchanged".
        [Required, MinLength(1)]
        public List<UpdateDashboardItemSourceDto> Sources { get; set; } = [];
    }

    public class UpdateDashboardItemSourceDtoValidator : AbstractValidator<UpdateDashboardItemSourceDto>
    {
        public UpdateDashboardItemSourceDtoValidator()
        {
            RuleFor(x => x.SourceId)
                .NotEmpty().WithMessage(x => Messages.Required("source id"));

            RuleFor(x => x.Label)
                .MaximumLength(100).WithMessage("Label cannot exceed 100 characters.")
                .When(x => !string.IsNullOrEmpty(x.Label));
        }
    }

    public class UpdateDashboardItemDtoValidator : AbstractValidator<UpdateDashboardItemDto>
    {
        public UpdateDashboardItemDtoValidator()
        {
            // Shape only; DB-dependent checks happen in DashboardService.
            RuleFor(x => x.Sources)
                .NotEmpty().WithMessage(x => Messages.Required("sources"));

            RuleFor(x => x.Name)
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.")
                .When(x => !string.IsNullOrEmpty(x.Name));

            RuleFor(x => x.GoalDirection)
                .Must(d => string.IsNullOrEmpty(d) || GoalDirections.IsValid(d))
                .WithMessage(x => Messages.Invalid("goal direction"));

            RuleFor(x => x.DisplayMode)
                .IsInEnum().WithMessage(x => Messages.Invalid("display mode"));
            RuleFor(x => x.MobileDisplayMode)
                .IsInEnum().WithMessage(x => Messages.Invalid("display mode"));

            RuleFor(x => x.Color)
                .MaximumLength(50).WithMessage("Color cannot exceed 50 characters.")
                .When(x => !string.IsNullOrEmpty(x.Color));

            RuleFor(x => x.CalendarStartMonth)
                .Must(v => string.IsNullOrEmpty(v) || CalendarStartMonths.IsValid(v))
                .WithMessage(x => Messages.Invalid("calendar start month"));

            RuleForEach(x => x.Sources)
                .SetValidator(new UpdateDashboardItemSourceDtoValidator());
        }
    }
}
