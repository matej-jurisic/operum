using FluentValidation;
using Operum.Model.Constants;
using System.ComponentModel.DataAnnotations;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    // Adds a widget by copying a tracker's own analytic instead of defining one inline. The
    // copy is taken at add time: the item still owns its definition, so editing or deleting
    // the tracker's analytic afterwards leaves the board as it was.
    public class AddDashboardItemFromAnalyticDto
    {
        [Required]
        public string AnalyticId { get; set; } = string.Empty;

        // Optional, and not copied from anywhere: a tracker's analytic is filtered by
        // whichever views the analytics page happens to have applied, so the board has to
        // be told which ones it should keep.
        public List<string> ViewIds { get; set; } = [];
    }

    public class AddDashboardItemFromAnalyticDtoValidator : AbstractValidator<AddDashboardItemFromAnalyticDto>
    {
        public AddDashboardItemFromAnalyticDtoValidator()
        {
            RuleFor(x => x.AnalyticId)
                .NotEmpty().WithMessage(x => Messages.Required("analytic id"));
        }
    }
}
