using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.DTOs.Queries;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    // Adds or edits a DashboardWidgetTypes.Filter item.
    //
    // Clauses are field-agnostic (pooled into Query rows on save) and always filters, never
    // sorts -- every clause is a value the board types in. Links carries the full set of
    // Analytic/Entries widgets that follow them with their per-clause field maps; each Links
    // entry's FieldByQuery is keyed by the clause's index in Clauses (the client has no
    // pooled query id until the save resolves one), and DashboardService rewrites those keys
    // to the pooled ids it stores.
    //
    // PresetIds names the board's DashboardViews this widget offers as presets. Each must be
    // a view on this board whose clause shape (data type + operator, in order) matches
    // Clauses exactly -- DashboardService checks that, along with everything else worth
    // checking (that a link names a real widget/tracker, that each mapped field matches its
    // clause).
    public class SaveFilterItemDto
    {
        public List<ClauseDto> Clauses { get; set; } = [];
        public List<WidgetLinkDto> Links { get; set; } = [];

        public List<string> PresetIds { get; set; } = [];
    }

    public class SaveFilterItemDtoValidator : AbstractValidator<SaveFilterItemDto>
    {
        public SaveFilterItemDtoValidator()
        {
            RuleFor(x => x.Clauses)
                .Must(c => c.Count > 0)
                .WithMessage(x => Messages.Required("clause"));

            RuleForEach(x => x.Clauses)
                .SetValidator(new ClauseDtoValidator())
                .Must(c => c.Kind == QueryKinds.Filter)
                    .WithMessage(x => Messages.Invalid("clause kind for a filter widget"));
        }
    }
}
