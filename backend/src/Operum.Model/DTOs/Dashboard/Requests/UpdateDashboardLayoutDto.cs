using FluentValidation;
using Operum.Model.Constants;
using System.ComponentModel.DataAnnotations;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    public class DashboardLayoutItemDto
    {
        [Required]
        public string ItemId { get; set; } = string.Empty;
        // Only honored on the wide grid (narrow grid flattens containers). An invalid parent
        // (nesting, or not a container on this board) is dropped to board-level.
        public string? ParentItemId { get; set; }
        // Only meaningful when ParentItemId names a TabsContainer. An unknown tab id is
        // dropped to board-level.
        public string? ParentTabId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }
    }

    // Ids not on the dashboard are ignored.
    public class UpdateDashboardLayoutDto
    {
        // Defaults to desktop so a client that predates the mobile grid doesn't overwrite it.
        public string Variant { get; set; } = DashboardLayoutVariants.Desktop;

        public List<DashboardLayoutItemDto> Items { get; set; } = [];
    }

    public class DashboardLayoutItemDtoValidator : AbstractValidator<DashboardLayoutItemDto>
    {
        public DashboardLayoutItemDtoValidator()
        {
            // Out-of-range placements are clamped in DashboardService, not rejected here.
            RuleFor(x => x.ItemId)
                .NotEmpty().WithMessage(x => Messages.Required("item id"));

            RuleFor(x => x.X).GreaterThanOrEqualTo(0).WithMessage(x => Messages.Invalid("x position"));
            RuleFor(x => x.Y).GreaterThanOrEqualTo(0).WithMessage(x => Messages.Invalid("y position"));
            RuleFor(x => x.W).GreaterThan(0).WithMessage(x => Messages.Invalid("width"));
            RuleFor(x => x.H).GreaterThan(0).WithMessage(x => Messages.Invalid("height"));
        }
    }

    public class UpdateDashboardLayoutDtoValidator : AbstractValidator<UpdateDashboardLayoutDto>
    {
        public UpdateDashboardLayoutDtoValidator()
        {
            // An unknown variant can't be clamped (no way to tell which grid it belongs to),
            // so the whole call is refused.
            RuleFor(x => x.Variant)
                .Must(DashboardLayoutVariants.IsValid).WithMessage(x => Messages.Invalid("layout variant"));

            RuleForEach(x => x.Items).SetValidator(new DashboardLayoutItemDtoValidator());
        }
    }
}
