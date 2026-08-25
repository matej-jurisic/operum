using FluentValidation;

namespace Operum.Model.DTOs.Analytics.Requests
{
    public class UpdateAnalyticDto
    {
        // Left unset (or blank), the analytic goes back to falling back to its
        // definition's own label the way it does when it was never named.
        public string? Name { get; set; }
    }

    public class UpdateAnalyticDtoValidator : AbstractValidator<UpdateAnalyticDto>
    {
        public UpdateAnalyticDtoValidator()
        {
            RuleFor(x => x.Name)
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.")
                .When(x => !string.IsNullOrEmpty(x.Name));
        }
    }
}
