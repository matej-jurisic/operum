using FluentValidation;

namespace Operum.Model.DTOs.Trackers.Requests
{
    public class RemoveUserFromTrackerDto
    {
        public string Username { get; set; } = string.Empty;
    }

    public class RemoveUserFromTrackerDtoValidator : AbstractValidator<RemoveUserFromTrackerDto>
    {
        public RemoveUserFromTrackerDtoValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required.");
        }
    }
}
