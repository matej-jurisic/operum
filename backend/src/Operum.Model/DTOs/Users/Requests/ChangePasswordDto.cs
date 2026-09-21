using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Users.Requests
{
    public class ChangePasswordDto
    {
        public required string CurrentPassword { get; set; }
        public required string NewPassword { get; set; }
    }

    public class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordDto>
    {
        public ChangePasswordDtoValidator()
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage(Messages.Required("current password"));

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage(Messages.Required("new password"))
                .MinimumLength(PasswordPolicy.MinLength).WithMessage($"Password must be at least {PasswordPolicy.MinLength} characters long.")
                .MaximumLength(PasswordPolicy.MaxLength).WithMessage($"Password must be at most {PasswordPolicy.MaxLength} characters long.");
        }
    }
}
