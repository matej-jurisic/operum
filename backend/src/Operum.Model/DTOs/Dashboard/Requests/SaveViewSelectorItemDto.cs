using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    // Adds or edits a DashboardWidgetTypes.ViewSelector item. The payload stands for the
    // whole widget: which DashboardViews it offers, the current selection, and the full set
    // of Analytic widgets that follow it with their per-clause field maps. Everything worth
    // checking beyond shape (that the options exist on this board, that a link names a real
    // Analytic widget and tracker, that each mapped field matches its clause's data type) is
    // settled in DashboardService.
    public class SaveViewSelectorItemDto
    {
        public List<string> OptionIds { get; set; } = [];
        public string? SelectedId { get; set; }
        public List<ViewSelectorLinkDto> Links { get; set; } = [];
    }

    public class SaveViewSelectorItemDtoValidator : AbstractValidator<SaveViewSelectorItemDto>
    {
        public SaveViewSelectorItemDtoValidator()
        {
            RuleFor(x => x.OptionIds)
                .NotEmpty().WithMessage(x => Messages.Required("option"));

            RuleForEach(x => x.OptionIds)
                .NotEmpty().WithMessage(x => Messages.Required("option id"));
        }
    }
}
