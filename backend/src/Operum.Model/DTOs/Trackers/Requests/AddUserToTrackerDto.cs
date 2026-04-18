using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Trackers.Requests
{
    public class AddUserToTrackerDto
    {
        public required string Username { get; set; } = string.Empty;
        public bool CanEditData { get; set; }
        public bool CanEditSchema { get; set; }
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
