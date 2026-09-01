using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.DTOs.Queries;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    // Adds or edits a DashboardWidgetTypes.Filter item. Two independent facets, each
    // optional on its own but at least one of which the widget must carry:
    //
    // Own clauses -- Clauses are field-agnostic (pooled into Query rows on save), each
    // carrying the value it currently filters on; Links carries the full set of
    // Analytic/Entries widgets that follow them with their per-clause field maps. Every
    // clause is a value the board types in, so they must all be filters, never sorts. Each
    // Links entry's FieldByQuery is keyed by the clause's index in Clauses (the client has
    // no pooled query id until the save resolves one); DashboardService rewrites those keys
    // to the pooled ids it stores.
    //
    // Presets -- PresetIds names the board's DashboardViews this widget offers as quick-apply
    // presets (filters AND sorts), SelectedPresetId the starting selection, and PresetLinks
    // the full set of followers for whichever preset is applied, keyed directly by pooled
    // DashboardViewQuery ids (no index rewrite needed, since presets already exist).
    //
    // Everything else worth checking (that a link names a real widget/tracker, that each
    // mapped field matches its clause, that PresetIds/SelectedPresetId name real views on
    // this board) is settled in DashboardService.
    public class SaveFilterItemDto
    {
        public List<ClauseDto> Clauses { get; set; } = [];
        public List<WidgetLinkDto> Links { get; set; } = [];

        public List<string> PresetIds { get; set; } = [];
        public string? SelectedPresetId { get; set; }
        public List<WidgetLinkDto> PresetLinks { get; set; } = [];
    }

    public class SaveFilterItemDtoValidator : AbstractValidator<SaveFilterItemDto>
    {
        public SaveFilterItemDtoValidator()
        {
            RuleFor(x => x)
                .Must(x => x.Clauses.Count > 0 || x.PresetIds.Count > 0)
                .WithMessage(x => Messages.Required("clause or preset"));

            RuleForEach(x => x.Clauses)
                .SetValidator(new ClauseDtoValidator())
                .Must(c => c.Kind == QueryKinds.Filter)
                    .WithMessage(x => Messages.Invalid("clause kind for a filter widget"));
        }
    }
}
