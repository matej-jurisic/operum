using FluentValidation;

namespace Operum.Model.DTOs.Views.Requests
{
    public class CreateViewDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public List<CreateViewSortDto> Sorts { get; set; } = [];
        public List<CreateViewFilterDto> Filters { get; set; } = [];
    }

    public class CreateViewDtoValidator : AbstractValidator<CreateViewDto>
    {
        private const int MaxSorts = 3;
        private const int MaxFilters = 6;

        public CreateViewDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("View name is required.")
                .MaximumLength(50).WithMessage("View name cannot exceed 50 characters.");

            RuleFor(x => x.Description)
               .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.")
               .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.Sorts)
                .Must(sorts => sorts.Count <= MaxSorts)
                    .WithMessage($"A maximum of {MaxSorts} sorts are allowed.")
                .Must(sorts => sorts.Select(s => s.FieldId).Distinct().Count() == sorts.Count)
                    .WithMessage("Each sort field must be unique.");

            RuleFor(x => x.Filters)
                .Must(filters => filters.Count <= MaxFilters)
                    .WithMessage($"A maximum of {MaxFilters} filters are allowed.")
                .Must(filters => filters.Distinct().Count() == filters.Count)
                    .WithMessage("Each filter must be unique.");

            RuleForEach(x => x.Sorts)
                .SetValidator(new CreateViewSortDtoValidator());
            RuleForEach(x => x.Filters)
                .SetValidator(new CreateViewFilterDtoValidator());
        }
    }
}
