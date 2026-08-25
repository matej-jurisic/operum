using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    // Adds a widget that opens a tracker's quick-add entry dialog from the board, rather
    // than rendering a chart. All it needs is which tracker the button is for.
    public class AddDashboardQuickAddItemDto
    {
        public string TrackerId { get; set; } = string.Empty;
    }

    public class AddDashboardQuickAddItemDtoValidator : AbstractValidator<AddDashboardQuickAddItemDto>
    {
        public AddDashboardQuickAddItemDtoValidator()
        {
            RuleFor(x => x.TrackerId)
                .NotEmpty().WithMessage(x => Messages.Required("tracker id"));
        }
    }
}
