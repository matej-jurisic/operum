using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.DTOs.Queries;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    // Creates or replaces a DashboardView -- a named set of filter clause values the board's
    // filter widgets can offer as a preset. The payload stands for the whole thing: the
    // clauses replace whatever was there. Clauses are field-agnostic (the field each runs
    // against is chosen per following widget) and must all be filters, never sorts -- a
    // preset is a value set, matched to a filter widget by its clause shape.
    public class SaveDashboardViewDto
    {
        public string Name { get; set; } = string.Empty;
        public List<ClauseDto> Clauses { get; set; } = [];
    }

    public class SaveDashboardViewDtoValidator : AbstractValidator<SaveDashboardViewDto>
    {
        public SaveDashboardViewDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(x => Messages.Required("name"))
                .MaximumLength(50).WithMessage("Name cannot exceed 50 characters.");

            RuleFor(x => x.Clauses)
                .NotEmpty().WithMessage(x => Messages.Required("clause"))
                .Must(c => c.Count <= DataLimits.MaxQueriesPerView)
                    .WithMessage(x => Messages.MaxNumberReached("clauses", DataLimits.MaxQueriesPerView));

            RuleForEach(x => x.Clauses)
                .SetValidator(new ClauseDtoValidator())
                .Must(c => c.Kind == QueryKinds.Filter)
                    .WithMessage(x => Messages.Invalid("clause kind for a preset"));
        }
    }
}
