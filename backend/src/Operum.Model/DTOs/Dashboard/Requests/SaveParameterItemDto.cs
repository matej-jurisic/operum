using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.DTOs.Queries;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    // Adds or edits a DashboardWidgetTypes.Parameter item. The payload stands for the whole
    // widget: its own filter clauses (field-agnostic, pooled into Query rows on save), each
    // carrying the value it currently filters on, plus the full set of Analytic/Entries
    // widgets that follow it with their per-clause field maps.
    //
    // A parameter widget has no dropdown -- every clause is a value the board types in -- so
    // clauses must all be filters, never sorts. Each Links entry's FieldByQuery is keyed by
    // the clause's index in Clauses (the client has no pooled query id until the save
    // resolves one); DashboardService rewrites those keys to the pooled ids it stores.
    // Everything else worth checking (that a link names a real widget/tracker, that each
    // mapped field matches its clause) is settled in DashboardService.
    public class SaveParameterItemDto
    {
        public List<ClauseDto> Clauses { get; set; } = [];
        public List<ViewSelectorLinkDto> Links { get; set; } = [];
    }

    public class SaveParameterItemDtoValidator : AbstractValidator<SaveParameterItemDto>
    {
        public SaveParameterItemDtoValidator()
        {
            RuleFor(x => x.Clauses)
                .NotEmpty().WithMessage(x => Messages.Required("clause"));

            RuleForEach(x => x.Clauses)
                .SetValidator(new ClauseDtoValidator())
                .Must(c => c.Kind == QueryKinds.Filter)
                    .WithMessage(x => Messages.Invalid("clause kind for a parameter widget"));
        }
    }
}
