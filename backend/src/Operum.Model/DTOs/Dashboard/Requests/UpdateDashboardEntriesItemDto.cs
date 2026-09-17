using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.Enums;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    // No TrackerId: changing the source tracker requires adding a new widget instead.
    public class UpdateDashboardEntriesItemDto
    {
        // Empty shows every field.
        public List<string> ColumnFieldIds { get; set; } = [];

        public DashboardItemDisplayMode DisplayMode { get; set; }
        public DashboardItemDisplayMode MobileDisplayMode { get; set; }
    }

    public class UpdateDashboardEntriesItemDtoValidator : AbstractValidator<UpdateDashboardEntriesItemDto>
    {
        public UpdateDashboardEntriesItemDtoValidator()
        {
            RuleFor(x => x.DisplayMode)
                .IsInEnum().WithMessage(x => Messages.Invalid("display mode"));
            RuleFor(x => x.MobileDisplayMode)
                .IsInEnum().WithMessage(x => Messages.Invalid("display mode"));
        }
    }
}
