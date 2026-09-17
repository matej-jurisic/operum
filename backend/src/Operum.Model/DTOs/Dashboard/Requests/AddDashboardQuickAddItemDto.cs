using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Dashboard.Requests
{
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
