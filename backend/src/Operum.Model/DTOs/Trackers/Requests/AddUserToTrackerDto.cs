using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Trackers.Requests
{
    public class AddUserToTrackerDto
    {
        public string Username { get; set; } = string.Empty;
    }

    public class AddUserToTrackerDtoValidator : AbstractValidator<AddUserToTrackerDto>
    {
        public AddUserToTrackerDtoValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage((x) => Messages.Required("username"));
        }
    }
}
