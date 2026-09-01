using FluentValidation;
using Operum.Model.Constants;
using System.ComponentModel.DataAnnotations;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    // Places an existing Widget Library Entries table onto this dashboard by reference --
    // see PlaceWidgetDto for the equivalent on a chart. The tracker it reads from is fixed
    // on the EntriesWidget itself; only the columns and layout are this placement's own.
    public class PlaceEntriesWidgetDto
    {
        [Required]
        public string EntriesWidgetId { get; set; } = string.Empty;

        // The tracker fields to show as columns, in order. Empty shows every field.
        public List<string> ColumnFieldIds { get; set; } = [];

        public bool Expandable { get; set; }
        public bool MobileExpandable { get; set; }
    }

    public class PlaceEntriesWidgetDtoValidator : AbstractValidator<PlaceEntriesWidgetDto>
    {
        public PlaceEntriesWidgetDtoValidator()
        {
            RuleFor(x => x.EntriesWidgetId)
                .NotEmpty().WithMessage(x => Messages.Required("entries widget id"));
        }
    }
}
