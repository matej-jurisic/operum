using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Auth.Requests
{
    public class LoginDto
    {
        public string Credentials { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequestDtoValidator : AbstractValidator<LoginDto>
    {
        public LoginRequestDtoValidator()
        {
            // Credentials rules
            RuleFor(x => x.Credentials)
                .NotEmpty().WithMessage(Messages.Required("credentials"))
                .MaximumLength(100).WithMessage("Credentials cannot exceed 100 characters.");

            // Password rules
            RuleFor(x => x.Password)
            .NotEmpty().WithMessage(Messages.Required("password"));
            //.MinimumLength(6).WithMessage("Password must be at least 6 characters long.")
            //.MaximumLength(100).WithMessage("Password cannot exceed 100 characters.")
            //.Matches(@"\d").WithMessage("Password must contain at least one digit.");
            //.Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            //.Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            //.Matches(@"[^\w\d\s]").WithMessage("Password must contain at least one non-alphanumeric character.");
        }
    }
}
