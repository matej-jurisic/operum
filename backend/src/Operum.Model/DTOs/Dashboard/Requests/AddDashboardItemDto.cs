using FluentValidation;
using Operum.Model.Constants;
using Operum.Model.Constants.Analytics;
using Operum.Model.DTOs.Analytics.Requests;
using System.ComponentModel.DataAnnotations;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    public class DashboardItemSourceRequestDto
    {
        [Required]
        public string TrackerId { get; set; } = string.Empty;

        // Exactly one of these two shapes must be supplied:
        //  - AnalyticId: reuse an analytic already saved on the tracker.
        //  - ResultType + Code + AnalyticFields: define the analytic inline, for this
        //    dashboard only. Lets a user build a multi-tracker chart without first having
        //    to create a matching analytic on every tracker involved.
        public string? AnalyticId { get; set; }

        public string? ResultType { get; set; }
        public string? Code { get; set; }
        public List<CreateAnalyticFieldDto> AnalyticFields { get; set; } = [];

        public List<string> ViewIds { get; set; } = [];
        public string? Label { get; set; }

        public bool IsAdHoc => string.IsNullOrWhiteSpace(AnalyticId);
    }

    public class AddDashboardItemDto
    {
        [Required, MinLength(1)]
        public List<DashboardItemSourceRequestDto> Sources { get; set; } = [];
    }

    public class DashboardItemSourceRequestDtoValidator : AbstractValidator<DashboardItemSourceRequestDto>
    {
        public DashboardItemSourceRequestDtoValidator()
        {
            RuleFor(x => x.TrackerId)
                .NotEmpty().WithMessage(x => Messages.Required("tracker id"));

            // Shape check only. Whether the referenced analytic/fields exist, belong to
            // the tracker, and carry compatible data types is settled in DashboardService,
            // which is the only place with database access.
            RuleFor(x => x)
                .Must(x => !x.IsAdHoc || (!string.IsNullOrWhiteSpace(x.ResultType) && !string.IsNullOrWhiteSpace(x.Code)))
                .WithMessage(x => Messages.Required("analytic id, or a result type and code"));

            RuleFor(x => x)
                .Must(x => x.IsAdHoc || (string.IsNullOrWhiteSpace(x.ResultType) && string.IsNullOrWhiteSpace(x.Code) && x.AnalyticFields.Count == 0))
                .WithMessage(x => Messages.NotAllowed("supplying both an analytic id and an inline analytic definition"));

            RuleFor(x => x.ResultType!)
                .Must(AnalyticTypes.IsValid).WithMessage(x => Messages.Invalid("result type"))
                .When(x => x.IsAdHoc && !string.IsNullOrWhiteSpace(x.ResultType));

            RuleFor(x => x.Code!)
                .Must(AnalyticCodes.IsValid).WithMessage(x => Messages.Invalid("code"))
                .When(x => x.IsAdHoc && !string.IsNullOrWhiteSpace(x.Code));

            RuleForEach(x => x.AnalyticFields)
                .SetValidator(new CreateAnalyticFieldDtoValidator());
        }
    }

    public class AddDashboardItemDtoValidator : AbstractValidator<AddDashboardItemDto>
    {
        public AddDashboardItemDtoValidator()
        {
            RuleFor(x => x.Sources)
                .NotEmpty().WithMessage(x => Messages.Required("sources"));

            RuleForEach(x => x.Sources)
                .SetValidator(new DashboardItemSourceRequestDtoValidator());
        }
    }
}
