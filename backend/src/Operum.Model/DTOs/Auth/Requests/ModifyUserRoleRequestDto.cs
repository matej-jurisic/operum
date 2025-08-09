using FluentValidation;

namespace Operum.Model.DTOs.Auth.Requests
{
    public class ModifyUserRoleRequestDto
    {
        public required string RoleName { get; set; }
    }

    public class ModifyUserRoleRequestDtoValidator : AbstractValidator<ModifyUserRoleRequestDto>
    {
        public ModifyUserRoleRequestDtoValidator()
        {
            RuleFor(x => x.RoleName)
                .NotEmpty().WithMessage("RoleName is required.");
        }
    }
}
