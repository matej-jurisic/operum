using FluentValidation;

namespace Operum.Model.DTOs.Widgets.Requests
{
    // Edits a widget in place, but only what isn't the definition itself: name and
    // description. The result type, code and source field mapping are fixed at creation --
    // changing those would silently turn every dashboard placing this widget into a
    // different chart. Create a new widget instead.
    public class UpdateWidgetDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
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
        }
    }
}
