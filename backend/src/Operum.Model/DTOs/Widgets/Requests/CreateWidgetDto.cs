using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.Constants.Analytics;
using Operum.Model.DTOs.Analytics.Requests;
using System.ComponentModel.DataAnnotations;

namespace Operum.Model.DTOs.Widgets.Requests
{
    public class CreateWidgetSourceRequestDto
    {
        [Required]
        public string TrackerId { get; set; } = string.Empty;

        // Fixed at creation -- see CreateWidgetDto.
        public List<CreateAnalyticFieldDto> Fields { get; set; } = [];
    }

    // Unlike the old per-tracker Analytic, sources can span more than one tracker; see MatchedValuesOnly.
    public class CreateWidgetDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }

        // Fixed at creation; create a new widget instead of changing this.
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

        // Goal widgets only. Null/empty behaves as HigherIsBetter.
        public string? GoalDirection { get; set; }

        [Required, MinLength(1)]
        public List<CreateWidgetSourceRequestDto> Sources { get; set; } = [];
    }

    public class CreateWidgetSourceRequestDtoValidator : AbstractValidator<CreateWidgetSourceRequestDto>
    {
        public CreateWidgetSourceRequestDtoValidator()
        {
            RuleFor(x => x.TrackerId)
                .NotEmpty().WithMessage(x => Messages.Required("tracker id"));

            RuleForEach(x => x.Fields)
                .SetValidator(new CreateAnalyticFieldDtoValidator());
        }
    }

    public class CreateWidgetDtoValidator : AbstractValidator<CreateWidgetDto>
    {
        public CreateWidgetDtoValidator()
        {
            RuleFor(x => x.Name)
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.")
                .When(x => !string.IsNullOrEmpty(x.Name));

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.")
                .When(x => !string.IsNullOrEmpty(x.Description));

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

            RuleForEach(x => x.Sources)
                .SetValidator(new CreateWidgetSourceRequestDtoValidator());
        }
    }
}
