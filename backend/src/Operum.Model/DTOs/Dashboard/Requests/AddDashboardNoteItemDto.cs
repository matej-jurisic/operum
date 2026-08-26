using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    // Adds a widget that is a free-form block of text, rather than a chart. Carries no
    // tracker or view — there is nothing to check but the text itself.
    public class AddDashboardNoteItemDto
    {
        public string Text { get; set; } = string.Empty;
    }

    public class AddDashboardNoteItemDtoValidator : AbstractValidator<AddDashboardNoteItemDto>
    {
        public AddDashboardNoteItemDtoValidator()
        {
            RuleFor(x => x.Text)
                .NotEmpty().WithMessage(x => Messages.Required("text"))
                .MaximumLength(DataLimits.MaxNoteTextLength)
                .WithMessage($"Text cannot exceed {DataLimits.MaxNoteTextLength} characters.");
        }
    }
}
