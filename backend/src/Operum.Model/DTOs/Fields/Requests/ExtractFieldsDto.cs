using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Fields.Requests
{
    /// <summary>
    /// Pulls a set of a tracker's fields out into a new tracker: the new tracker gets a copy
    /// of each field and one entry per distinct combination of their values, and the source
    /// tracker keeps a single <c>reference</c> field in their place, linked to the matching
    /// new-tracker entry on every existing row.
    /// </summary>
    public class ExtractFieldsDto
    {
        public required List<string> FieldIds { get; set; } = [];

        public required string NewTrackerName { get; set; } = string.Empty;

        public required string ReferenceFieldName { get; set; } = string.Empty;

        /// <summary>
        /// Which of <see cref="FieldIds"/> is shown as the link label. Null falls back to the
        /// linked entry's creation date, the same as any other reference field.
        /// </summary>
        public string? DisplayFieldId { get; set; }
    }

    public class ExtractFieldsDtoValidator : AbstractValidator<ExtractFieldsDto>
    {
        public ExtractFieldsDtoValidator()
        {
            RuleFor(x => x.FieldIds)
                .NotEmpty().WithMessage("Pick at least one field to extract.");

            RuleFor(x => x.FieldIds)
                .Must(ids => ids.Count == ids.Distinct().Count())
                .WithMessage("A field cannot be extracted twice.")
                .When(x => x.FieldIds.Count > 0);

            RuleFor(x => x.NewTrackerName)
                .NotEmpty().WithMessage("Tracker name is required.")
                .MaximumLength(100).WithMessage("Tracker name cannot exceed 100 characters.");

            RuleFor(x => x.ReferenceFieldName)
                .NotEmpty().WithMessage((x) => Messages.Required("field name"))
                .MaximumLength(30).WithMessage("Field name cannot exceed 30 characters.");

            RuleFor(x => x.DisplayFieldId)
                .Must((dto, displayFieldId) => dto.FieldIds.Contains(displayFieldId!))
                .WithMessage("The label field must be one of the extracted fields.")
                .When(x => !string.IsNullOrEmpty(x.DisplayFieldId));
        }
    }
}
