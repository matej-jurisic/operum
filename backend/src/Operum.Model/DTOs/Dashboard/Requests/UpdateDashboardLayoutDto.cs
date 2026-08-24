using FluentValidation;
using Operum.Model.Constants;
using System.ComponentModel.DataAnnotations;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    public class DashboardLayoutItemDto
    {
        [Required]
        public string ItemId { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }
    }

    // The whole board's placement in one call: the grid hands back every item it holds
    // after a drag or a resize, and ids that are not on the dashboard are ignored.
    public class UpdateDashboardLayoutDto
    {
        public List<DashboardLayoutItemDto> Items { get; set; } = [];
    }

    public class DashboardLayoutItemDtoValidator : AbstractValidator<DashboardLayoutItemDto>
    {
        public DashboardLayoutItemDtoValidator()
        {
            // Only the shape is checked here. A placement that is merely awkward (off the
            // right edge, taller than the board allows) is clamped in DashboardService
            // rather than rejected, so a client with its own idea of the grid still saves.
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
            RuleForEach(x => x.Items).SetValidator(new DashboardLayoutItemDtoValidator());
        }
    }
}
