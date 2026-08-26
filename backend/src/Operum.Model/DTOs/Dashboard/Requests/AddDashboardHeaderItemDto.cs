using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    // Adds a widget that is a short line of text read as a section title, rather than a
    // chart. Carries no tracker or view — there is nothing to check but the text itself.
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
