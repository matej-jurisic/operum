using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    // Replaces the whole tab list: a matching Id is renamed in place, a missing/unknown Id is
    // created, and a tab absent here is removed with its children repointed to the first
    // surviving tab (see DashboardService.SaveTabsContainer).
    public class SaveTabsContainerDto
    {
        public string? Title { get; set; }
        public List<SaveTabDto> Tabs { get; set; } = [];
    }

    public class SaveTabDto
    {
        public string? Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class SaveTabsContainerDtoValidator : AbstractValidator<SaveTabsContainerDto>
    {
        public SaveTabsContainerDtoValidator()
        {
            RuleFor(x => x.Title)
                .MaximumLength(DataLimits.MaxHeaderTextLength)
                .WithMessage($"Title cannot exceed {DataLimits.MaxHeaderTextLength} characters.");

            RuleFor(x => x.Tabs)
                .Must(t => t.Count is >= 1 and <= DataLimits.MaxDashboardTabCount)
                .WithMessage($"A tabs container must have between 1 and {DataLimits.MaxDashboardTabCount} tabs.");

            RuleForEach(x => x.Tabs).ChildRules(tab =>
            {
                tab.RuleFor(t => t.Name)
                    .NotEmpty().WithMessage(x => Messages.Required("tab name"))
                    .MaximumLength(DataLimits.MaxTabNameLength)
                    .WithMessage($"A tab name cannot exceed {DataLimits.MaxTabNameLength} characters.");
            });
        }
    }
}
