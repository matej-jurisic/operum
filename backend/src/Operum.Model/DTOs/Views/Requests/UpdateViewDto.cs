using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Views.Requests
{
    public class UpdateViewDto
    {
        public required string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public List<ViewQueryRefDto> Queries { get; set; } = [];
    }

    public class UpdateViewDtoValidator : AbstractValidator<UpdateViewDto>
    {
        public UpdateViewDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage((x) => Messages.Required("name"))
                .MaximumLength(50).WithMessage("View name cannot exceed 50 characters.");

            RuleFor(x => x.Description)
               .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.")
               .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.Queries)
                .Must(queries => queries.Count <= DataLimits.MaxQueriesPerView)
                    .WithMessage((x) => Messages.MaxNumberReached("queries", DataLimits.MaxQueriesPerView))
                .Must(queries => queries.Where(q => q.QueryId != null).Select(q => q.QueryId).Distinct().Count()
                    == queries.Count(q => q.QueryId != null))
                    .WithMessage("Each existing query can only be added to a view once.");

            RuleForEach(x => x.Queries)
                .SetValidator(new ViewQueryRefDtoValidator());
        }
    }
}
