using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Auth.Requests
{
    public class ConfirmEmailDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }

    public class ConfirmEmailDtoValidator : AbstractValidator<ConfirmEmailDto>
    {
        public ConfirmEmailDtoValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage(Messages.Required("user id"));

            RuleFor(x => x.Token)
                .NotEmpty().WithMessage(Messages.Required("token"));
        }
    }
}
