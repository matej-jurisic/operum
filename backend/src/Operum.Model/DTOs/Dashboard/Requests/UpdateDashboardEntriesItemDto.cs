using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.Enums;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    // No TrackerId: changing the source tracker requires adding a new widget instead.
    public class UpdateDashboardEntriesItemDto
    {
        // Lives on the shared EntriesWidget, not the placement. Cleared when left blank,
        // same as at creation.
        public string? Name { get; set; }

        // Empty shows every field.
        public List<string> ColumnFieldIds { get; set; } = [];

        public DashboardItemDisplayMode DisplayMode { get; set; }
        public DashboardItemDisplayMode MobileDisplayMode { get; set; }
    }

    public class UpdateDashboardEntriesItemDtoValidator : AbstractValidator<UpdateDashboardEntriesItemDto>
    {
        public UpdateDashboardEntriesItemDtoValidator()
        {
            RuleFor(x => x.Name)
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.")
                .When(x => !string.IsNullOrEmpty(x.Name));

            RuleFor(x => x.DisplayMode)
                .IsInEnum().WithMessage(x => Messages.Invalid("display mode"));
            RuleFor(x => x.MobileDisplayMode)
                .IsInEnum().WithMessage(x => Messages.Invalid("display mode"));
        }
    }
}
