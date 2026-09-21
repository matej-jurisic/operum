using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Auth.Requests
{
    public class RegisterDto
    {
        public required string Email { get; set; } = string.Empty;
        public required string UserName { get; set; } = string.Empty;
        public required string Password { get; set; } = string.Empty;
    }

    public class RegisterRequestDtoValidator : AbstractValidator<RegisterDto>
    {
        public RegisterRequestDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage((x) => Messages.Required("email"))
                .MaximumLength(100).WithMessage("Email cannot exceed 100 characters.")
                .EmailAddress().WithMessage("Invalid email format.");

            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage((x) => Messages.Required("username"))
                .MinimumLength(3).WithMessage("Username must be at least 3 characters long.")
                .MaximumLength(20).WithMessage("Username cannot exceed 20 characters.");

            // Length only; the common-password and account-match checks live in PasswordPolicyValidator.
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage((x) => Messages.Required("password"))
                .MinimumLength(PasswordPolicy.MinLength).WithMessage($"Password must be at least {PasswordPolicy.MinLength} characters long.")
                .MaximumLength(PasswordPolicy.MaxLength).WithMessage($"Password must be at most {PasswordPolicy.MaxLength} characters long.");
        }
    }
}
