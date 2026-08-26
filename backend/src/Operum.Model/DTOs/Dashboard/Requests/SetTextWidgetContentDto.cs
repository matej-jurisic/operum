using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    // Changes what a DashboardWidgetTypes.Header or DashboardWidgetTypes.Note widget's text
    // reads. Persisted onto the item's own Config, the same as a View widget's selection, so
    // it's what every future load starts from — not just this session.
    //
    // Shared by both widgets rather than split in two, since editing either one is exactly
    // the same operation; the length cap that differs between them (DataLimits.
    // MaxHeaderTextLength vs MaxNoteTextLength) is enforced in DashboardService, which is
    // the only place that knows which of the two this item actually is.
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
