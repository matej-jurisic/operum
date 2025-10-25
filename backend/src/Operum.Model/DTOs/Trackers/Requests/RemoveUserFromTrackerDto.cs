using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Trackers.Requests
{
    public class RemoveUserFromTrackerDto
    {
        public required string Username { get; set; } = string.Empty;
    }

    public class RemoveUserFromTrackerDtoValidator : AbstractValidator<RemoveUserFromTrackerDto>
    {
        public RemoveUserFromTrackerDtoValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage((x) => Messages.Required("username"));
        }
    }
}
