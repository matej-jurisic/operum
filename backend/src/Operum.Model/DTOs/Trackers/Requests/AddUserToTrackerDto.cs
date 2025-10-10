using FluentValidation;

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
                .NotEmpty().WithMessage("Username is required.")
                .MinimumLength(3).WithMessage("Username must be at least 3 characters long.")
                .MaximumLength(20).WithMessage("Username cannot exceed 20 characters.");
        }
    }
}
