using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Users.Requests
{
    public class ChangeUserRoleDto
    {
        public required string RoleName { get; set; }
    }

    public class ChangeUserRoleDtoValidator : AbstractValidator<ChangeUserRoleDto>
    {
        public ChangeUserRoleDtoValidator()
        {
            RuleFor(x => x.RoleName)
                .NotEmpty().WithMessage((x) => Messages.Required("role name"));
        }
    }
}
