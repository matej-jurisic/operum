using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Auth.Requests
{
    public class GoogleLoginDto
    {
        public required string IdToken { get; set; } = string.Empty;
    }

    public class GoogleLoginDtoValidator : AbstractValidator<GoogleLoginDto>
    {
        public GoogleLoginDtoValidator()
        {
            RuleFor(x => x.IdToken)
                .NotEmpty().WithMessage((x) => Messages.Required("credential"));
        }
    }
}
