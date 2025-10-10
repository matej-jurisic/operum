using FluentValidation;

namespace Operum.Model.DTOs.Auth.Requests
{
    public class ChangeUserRoleDto
    {
        public required string RoleName { get; set; }
    }

    public class ModifyUserRoleRequestDtoValidator : AbstractValidator<ChangeUserRoleDto>
    {
        public ModifyUserRoleRequestDtoValidator()
        {
            RuleFor(x => x.RoleName)
                .NotEmpty().WithMessage("RoleName is required.");
        }
    }
}
