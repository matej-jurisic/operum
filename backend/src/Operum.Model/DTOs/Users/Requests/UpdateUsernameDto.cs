using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Users.Requests
{
    public class UpdateUsernameDto
    {
        public required string UserName { get; set; }
    }

    public class UpdateUsernameDtoValidator : AbstractValidator<UpdateUsernameDto>
    {
        public UpdateUsernameDtoValidator()
        {
            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage(Messages.Required("username"))
                .MinimumLength(3).WithMessage("Username must be at least 3 characters long.")
                .MaximumLength(20).WithMessage("Username cannot exceed 20 characters.");
        }
    }
}
