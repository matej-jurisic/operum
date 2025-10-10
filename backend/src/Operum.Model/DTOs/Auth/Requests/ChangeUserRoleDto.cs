using FluentValidation;
using Operum.Model.Constants;

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
                .NotEmpty().WithMessage(Messages.Required("role name"));
        }
    }
}
