using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Auth.Requests
{
    public class ConfirmEmailDto
    {
        public required string UserId { get; set; } = string.Empty;
        public required string Token { get; set; } = string.Empty;
    }

    public class ConfirmEmailDtoValidator : AbstractValidator<ConfirmEmailDto>
    {
        public ConfirmEmailDtoValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage((x) => Messages.Required("user id"));

            RuleFor(x => x.Token)
                .NotEmpty().WithMessage((x) => Messages.Required("token"));
        }
    }
}
