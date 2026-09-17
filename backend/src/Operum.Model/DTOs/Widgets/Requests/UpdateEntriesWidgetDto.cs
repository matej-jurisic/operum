using FluentValidation;

namespace Operum.Model.DTOs.Widgets.Requests
{
    // Tracker is fixed at creation; only the name is editable afterwards.
    public class UpdateEntriesWidgetDto
    {
        public string? Name { get; set; }
    }

    public class UpdateEntriesWidgetDtoValidator : AbstractValidator<UpdateEntriesWidgetDto>
    {
        public UpdateEntriesWidgetDtoValidator()
        {
            RuleFor(x => x.Name)
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.")
                .When(x => !string.IsNullOrEmpty(x.Name));
        }
    }
}
