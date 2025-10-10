using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Auth.Requests
{
    public class RegisterDto
    {
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequestDtoValidator : AbstractValidator<RegisterDto>
    {
        public RegisterRequestDtoValidator()
        {
            // Email rules
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(Messages.Required("email"))
                .MaximumLength(100).WithMessage("Email cannot exceed 100 characters.")
                .EmailAddress().WithMessage("Invalid email format.");

            // Username rules
            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage(Messages.Required("username"))
                .MinimumLength(3).WithMessage("Username must be at least 3 characters long.")
                .MaximumLength(20).WithMessage("Username cannot exceed 20 characters.");

            // Password rules (matches ASP.NET Identity defaults)
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage(Messages.Required("password"))
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long.")
                //.Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                //.Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                //.Matches(@"[^\w\d\s]").WithMessage("Password must contain at least one non-alphanumeric character.");
                .Matches(@"\d").WithMessage("Password must contain at least one digit.");
        }
    }
}
