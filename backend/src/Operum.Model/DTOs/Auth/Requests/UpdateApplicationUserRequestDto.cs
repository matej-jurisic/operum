using FluentValidation;

namespace Operum.Model.DTOs.Auth.Requests
{
    public class UpdateApplicationUserRequestDto
    {
        public string UserName { get; set; } = string.Empty;
    }

    public class UpdateApplicationUserRequestDtoValidator : AbstractValidator<UpdateApplicationUserRequestDto>
    {
        public UpdateApplicationUserRequestDtoValidator()
        {
            RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters long.")
            .MaximumLength(20).WithMessage("Username must be at most 20 characters long.");
        }
    }
}
