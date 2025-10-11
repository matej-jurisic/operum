using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.Enums;

namespace Operum.Model.DTOs.Trackers.Requests
{
    public class UpdateTrackerDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Color { get; set; }
        public int? TrackerTypeId { get; set; } = null;
    }

    public class UpdateTrackerDtoValidator : AbstractValidator<UpdateTrackerDto>
    {
        public UpdateTrackerDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tracker name is required.")
                .MaximumLength(100).WithMessage("Tracker name cannot exceed 100 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.")
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.TrackerTypeId)
                .Must(x => !x.HasValue || Enum.IsDefined(typeof(PublicityEnum), x.Value))
                .WithMessage((x) => Messages.Invalid("tracker type"));

            RuleFor(x => x.Color)
                .MaximumLength(50)
                .WithMessage("Color cannot exceed 50 characters.");
        }
    }
}
