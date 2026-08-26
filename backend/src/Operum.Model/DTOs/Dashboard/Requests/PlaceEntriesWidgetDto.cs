using FluentValidation;
using Operum.Model.Constants;
using System.ComponentModel.DataAnnotations;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    // Places an existing Widget Library Entries table onto this dashboard by reference --
    // see PlaceWidgetDto for the equivalent on a chart. The tracker it reads from is fixed
    // on the EntriesWidget itself; only the filter and layout are this placement's own.
    public class PlaceEntriesWidgetDto
    {
        [Required]
        public string EntriesWidgetId { get; set; } = string.Empty;

        public string? ViewId { get; set; }
        public string? LinkedViewWidgetId { get; set; }

        public bool Expandable { get; set; }
        public bool MobileExpandable { get; set; }
    }

    public class PlaceEntriesWidgetDtoValidator : AbstractValidator<PlaceEntriesWidgetDto>
    {
        public PlaceEntriesWidgetDtoValidator()
        {
            RuleFor(x => x.EntriesWidgetId)
                .NotEmpty().WithMessage(x => Messages.Required("entries widget id"));

            RuleFor(x => x)
                .Must(x => string.IsNullOrEmpty(x.ViewId) || string.IsNullOrEmpty(x.LinkedViewWidgetId))
                .WithMessage("An entries widget cannot both filter by a fixed view and follow a view widget.");
        }
    }
}
