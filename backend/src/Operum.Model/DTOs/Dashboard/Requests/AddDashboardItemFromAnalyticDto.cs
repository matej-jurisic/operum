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
        // whichever view the analytics page happens to have applied, so the board has to
        // be told which one it should keep. At most one of these — see
        // DashboardItemSourceRequestDto.
        public string? ViewId { get; set; }
        public string? LinkedViewWidgetId { get; set; }
    }

    public class AddDashboardItemFromAnalyticDtoValidator : AbstractValidator<AddDashboardItemFromAnalyticDto>
    {
        public AddDashboardItemFromAnalyticDtoValidator()
        {
            RuleFor(x => x.AnalyticId)
                .NotEmpty().WithMessage(x => Messages.Required("analytic id"));

            RuleFor(x => x)
                .Must(x => string.IsNullOrEmpty(x.ViewId) || string.IsNullOrEmpty(x.LinkedViewWidgetId))
                .WithMessage("A source cannot both filter by a fixed view and follow a view widget.");
        }
    }
}
