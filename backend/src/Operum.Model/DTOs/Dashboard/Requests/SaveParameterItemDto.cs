using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    // Adds or edits a DashboardWidgetTypes.Parameter item. The payload stands for the whole
    // widget: the single DashboardView it drives, the current per-clause values, and the
    // full set of Analytic/Entries widgets that follow it with their per-clause field maps.
    // Everything worth checking beyond shape (that the view exists on this board, that each
    // value is valid for its clause's data type, that a link names a real widget/tracker and
    // each mapped field matches its clause) is settled in DashboardService.
    public class SaveParameterItemDto
    {
        public string ViewId { get; set; } = string.Empty;
        public Dictionary<string, string?> Values { get; set; } = [];
        public List<ViewSelectorLinkDto> Links { get; set; } = [];
    }

    public class SaveParameterItemDtoValidator : AbstractValidator<SaveParameterItemDto>
    {
        public SaveParameterItemDtoValidator()
        {
            RuleFor(x => x.ViewId)
                .NotEmpty().WithMessage(x => Messages.Required("filter set"));
        }
    }
}
