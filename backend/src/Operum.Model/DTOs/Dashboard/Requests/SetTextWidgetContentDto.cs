using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    // Shared by Header, Note, and Container title edits; the length cap that differs between
    // them is enforced in DashboardService, the only place that knows which one this item is.
    public class SetTextWidgetContentDto
    {
        public string Text { get; set; } = string.Empty;
    }

    public class SetTextWidgetContentDtoValidator : AbstractValidator<SetTextWidgetContentDto>
    {
        public SetTextWidgetContentDtoValidator()
        {
            RuleFor(x => x.Text)
                .NotEmpty().WithMessage(x => Messages.Required("text"));
        }
    }
}
