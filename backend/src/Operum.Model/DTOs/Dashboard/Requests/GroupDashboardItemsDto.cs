using FluentValidation;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    // Creates a Container and tags every named item as its member in one step (see
    // DashboardService.GroupItems). A Container owns no placement of its own -- members
    // keep whatever board-relative X/Y they already had.
    public class GroupDashboardItemsDto
    {
        public List<string> ItemIds { get; set; } = [];
    }

    public class GroupDashboardItemsDtoValidator : AbstractValidator<GroupDashboardItemsDto>
    {
        public GroupDashboardItemsDtoValidator()
        {
            RuleFor(x => x.ItemIds)
                .Must(ids => ids.Count >= 2)
                .WithMessage("A group needs at least 2 items.")
                .Must(ids => ids.Distinct().Count() == ids.Count)
                .WithMessage("Duplicate item in group selection.");
        }
    }
}
