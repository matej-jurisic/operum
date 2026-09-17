using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.Constants.Analytics;

namespace Operum.Model.DTOs.Widgets.Requests
{
    // Result type, code and source field mapping are fixed at creation; create a new widget instead.
    public class UpdateWidgetDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        // Goal widgets only. Left null, the existing target is kept.
        public string? GoalTarget { get; set; }
        // Goal widgets only. Left null, the existing direction is kept.
        public string? GoalDirection { get; set; }
    }

    public class UpdateWidgetDtoValidator : AbstractValidator<UpdateWidgetDto>
    {
        public UpdateWidgetDtoValidator()
        {
            RuleFor(x => x.Name)
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.")
                .When(x => !string.IsNullOrEmpty(x.Name));

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.")
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.GoalDirection)
                .Must(d => string.IsNullOrEmpty(d) || GoalDirections.IsValid(d))
                .WithMessage(x => Messages.Invalid("goal direction"));
        }
    }
}
