using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Auth.Requests
{
    public class GoogleLoginDto
    {
        public string Credential { get; set; } = string.Empty;
    }

    public class GoogleLoginDtoValidator : AbstractValidator<GoogleLoginDto>
    {
        public GoogleLoginDtoValidator()
        {
            RuleFor(x => x.Credential)
                .NotEmpty().WithMessage((x) => Messages.Required("credential"));
        }
    }
}
