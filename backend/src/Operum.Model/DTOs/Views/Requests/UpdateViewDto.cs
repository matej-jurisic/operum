using FluentValidation;
using Operum.Model.Constants;

namespace Operum.Model.DTOs.Views.Requests
{
    public class UpdateViewDto
    {
        public required string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public List<ViewClauseDto> Queries { get; set; } = [];

        // The fields this view shows, in the order it shows them. Empty means every field,
        // which is what every view did before columns existed.
        public List<string> ColumnFieldIds { get; set; } = [];
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
                    .WithMessage((x) => Messages.MaxNumberReached("queries", DataLimits.MaxQueriesPerView));

            RuleFor(x => x.ColumnFieldIds)
                .Must(ids => ids.Count <= DataLimits.MaxColumns)
                    .WithMessage((x) => Messages.MaxNumberReached("columns", DataLimits.MaxColumns));

            RuleForEach(x => x.Queries)
                .SetValidator(new ViewClauseDtoValidator());
        }
    }
}
