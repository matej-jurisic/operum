using FluentValidation;

namespace Operum.Model.DTOs.Entries.Requests
{
    public class CreateEntryDto
    {
        public Dictionary<string, string?> FieldValues { get; set; } = [];
    }

    public class CreateEntryDtoValidator : AbstractValidator<CreateEntryDto>
    {
        public CreateEntryDtoValidator()
        {
            RuleFor(x => x.FieldValues)
                .NotNull().WithMessage("FieldValues is required.")
                .Must(d => d.Count != 0).WithMessage("At least one field value is required.")
                .Must(d => d.Count <= 20).WithMessage("Too many fields provided (max 20).");

            RuleForEach(x => x.FieldValues.Values)
                .Must(v => v == null || v.Length <= 1000)
                .WithMessage("Field value cannot exceed 1000 characters.");
        }
    }
}
