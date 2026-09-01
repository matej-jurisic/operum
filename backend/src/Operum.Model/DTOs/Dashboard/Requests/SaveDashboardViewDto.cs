using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.DTOs.Queries;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    // Creates or replaces a DashboardView -- a named clause set the board's view selectors
    // can offer. The payload stands for the whole thing: the clauses replace whatever was
    // there. Clauses are field-agnostic; the field each runs against is chosen per following
    // widget on the selector, not here.
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

            RuleForEach(x => x.Clauses).SetValidator(new ClauseDtoValidator());
        }
    }
}
