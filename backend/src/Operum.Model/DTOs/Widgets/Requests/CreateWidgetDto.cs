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

        // Which of the tracker's fields fill the purposes required by the widget's
        // ResultType + Code. Fixed at creation -- see CreateWidgetDto.
        public List<CreateAnalyticFieldDto> Fields { get; set; } = [];
    }

    // Defines a new, reusable Widget Library chart. Unlike the old per-tracker Analytic,
    // sources can span more than one tracker -- see MatchedValuesOnly.
    public class CreateWidgetDto
    {
        // Optional: left unset, the widget falls back to its definition's own label
        // (e.g. "Sum") the way a tracker analytic used to.
        public string? Name { get; set; }
        public string? Description { get; set; }

        // One definition for the whole widget: every source is calculated the same way, so
        // the series of a multi-tracker chart always share an axis semantics. Fixed at
        // creation -- create a new widget instead of changing this one.
        [Required]
        public string ResultType { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;

        // Combined charts only: keep just the x-axis values every source has a point for,
        // so the series line up over the same range. A single-source widget ignores it.
        public bool MatchedValuesOnly { get; set; }

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

            // Shape check only. Whether the code goes with the result type, and whether the
            // fields exist, belong to their tracker and carry compatible data types, is
            // settled in WidgetsService, which is the only place with database access.
            RuleFor(x => x.ResultType)
                .NotEmpty().WithMessage(x => Messages.Required("result type"))
                .Must(AnalyticTypes.IsValid).WithMessage(x => Messages.Invalid("result type"));

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage(x => Messages.Required("code"))
                .Must(AnalyticCodes.IsValid).WithMessage(x => Messages.Invalid("code"));

            RuleFor(x => x.Sources)
                .NotEmpty().WithMessage(x => Messages.Required("sources"));

            RuleForEach(x => x.Sources)
                .SetValidator(new CreateWidgetSourceRequestDtoValidator());
        }
    }
}
