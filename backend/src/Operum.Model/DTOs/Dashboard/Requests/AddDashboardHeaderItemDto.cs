using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    public class AddDashboardHeaderItemDto
    {
        public string Text { get; set; } = string.Empty;
    }

    public class AddDashboardHeaderItemDtoValidator : AbstractValidator<AddDashboardHeaderItemDto>
    {
        public AddDashboardHeaderItemDtoValidator()
        {
            RuleFor(x => x.Text)
                .NotEmpty().WithMessage(x => Messages.Required("text"))
                .MaximumLength(DataLimits.MaxHeaderTextLength)
                .WithMessage($"Text cannot exceed {DataLimits.MaxHeaderTextLength} characters.");
        }
    }
}
