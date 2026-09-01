using FluentValidation;
using Operum.Model.Constants;
using System.ComponentModel.DataAnnotations;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    // Defines a new Widget Library Entries table and places it on this dashboard in one
    // call -- the single-round-trip convenience EntriesWidgetForm relies on. See
    // CreateAndPlaceWidgetDto for the equivalent on a chart.
    public class CreateAndPlaceEntriesWidgetDto
    {
        [Required]
        public string TrackerId { get; set; } = string.Empty;
        public string? Name { get; set; }

        public string? ViewId { get; set; }

        public bool Expandable { get; set; }
        public bool MobileExpandable { get; set; }
    }

    public class CreateAndPlaceEntriesWidgetDtoValidator : AbstractValidator<CreateAndPlaceEntriesWidgetDto>
    {
        public CreateAndPlaceEntriesWidgetDtoValidator()
        {
            RuleFor(x => x.TrackerId)
                .NotEmpty().WithMessage(x => Messages.Required("tracker id"));
        }
    }
}
