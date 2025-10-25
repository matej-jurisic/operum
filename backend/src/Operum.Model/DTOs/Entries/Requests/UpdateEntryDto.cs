using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Entries.Requests
{
    public class UpdateEntryDto
    {
        public required Dictionary<string, string?> FieldValues { get; set; } = [];
    }

    public class UpdateEntryDtoValidator : AbstractValidator<UpdateEntryDto>
    {
        public UpdateEntryDtoValidator()
        {
            RuleFor(x => x.FieldValues)
                .NotNull().WithMessage((x) => Messages.Required("fieldValues"))
                .Must(d => d.Count != 0).WithMessage("At least one field value is required.")
                .Must(d => d.Count <= 20).WithMessage("Too many fields provided (max 20).");

            RuleForEach(x => x.FieldValues.Values)
                .Must(v => v == null || v.Length <= 1000)
                .WithMessage("Field value cannot exceed 1000 characters.");
        }
    }
}
