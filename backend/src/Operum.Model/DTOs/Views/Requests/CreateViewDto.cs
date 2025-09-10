using FluentValidation;

namespace Operum.Model.DTOs.Views.Requests
{
    public class CreateViewDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public List<CreateViewSortDto> Sorts { get; set; } = [];
    }

    public class CreateViewDtoValidator : AbstractValidator<CreateViewDto>
    {
        private const int MaxSorts = 3;

        public CreateViewDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("View name is required.")
                .MaximumLength(50).WithMessage("View name cannot exceed 50 characters.");

            RuleFor(x => x.Description)
               .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.")
               .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.Sorts)
                .NotEmpty().WithMessage("At least one sort is required.")
                .Must(sorts => sorts.Count <= MaxSorts)
                    .WithMessage($"A maximum of {MaxSorts} sorts are allowed.")
                .Must(sorts => sorts.Select(s => s.FieldId).Distinct().Count() == sorts.Count)
                    .WithMessage("Each sort field must be unique.");

            RuleForEach(x => x.Sorts)
                .SetValidator(new CreateViewSortDtoValidator());
        }
    }
}
