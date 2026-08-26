using FluentValidation;
using Operum.Model.Constants;
using System.ComponentModel.DataAnnotations;

namespace Operum.Model.DTOs.Widgets.Requests
{
    public class CreateEntriesWidgetDto
    {
        [Required]
        public string TrackerId { get; set; } = string.Empty;
        public string? Name { get; set; }
    }

    public class CreateEntriesWidgetDtoValidator : AbstractValidator<CreateEntriesWidgetDto>
    {
        public CreateEntriesWidgetDtoValidator()
        {
            RuleFor(x => x.TrackerId)
                .NotEmpty().WithMessage(x => Messages.Required("tracker id"));

            RuleFor(x => x.Name)
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.")
                .When(x => !string.IsNullOrEmpty(x.Name));
        }
    }
}
