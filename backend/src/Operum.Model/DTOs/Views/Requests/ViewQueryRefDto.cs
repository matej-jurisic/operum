using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.DTOs.Queries.Requests;

namespace Operum.Model.DTOs.Views.Requests
{
    // One entry in a View's ordered Query list: either a reference to an existing,
    // reusable Query, or a brand-new one authored inline while building the View
    // (which is still saved as a real, independently reusable Query row).
    public class ViewQueryRefDto
    {
        public string? QueryId { get; set; }
        public CreateQueryDto? NewQuery { get; set; }
    }

    public class ViewQueryRefDtoValidator : AbstractValidator<ViewQueryRefDto>
    {
        public ViewQueryRefDtoValidator()
        {
            RuleFor(x => x)
                .Must(x => (x.QueryId != null) ^ (x.NewQuery != null))
                .WithMessage("Each view query must reference exactly one of an existing query or a new query.");

            RuleFor(x => x.NewQuery!)
                .SetValidator(new CreateQueryDtoValidator())
                .When(x => x.NewQuery != null);
        }
    }
}
