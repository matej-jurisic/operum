using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.DTOs.Queries;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    // Clauses must all be filters, never sorts. Each Links entry's FieldByQuery is keyed by
    // the clause's index in Clauses (no stable id exists until save resolves one);
    // DashboardService rewrites those keys to the per-clause SlotIds it stores. PresetIds
    // must name views on this board whose clause shape matches Clauses exactly.
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
