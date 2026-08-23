using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    public class DashboardItemSource
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public int Order { get; set; }
        public string? Label { get; set; }
        public string? ViewIds { get; set; }

        public string DashboardItemId { get; set; } = string.Empty;
        [ForeignKey(nameof(DashboardItemId))]
        public virtual DashboardItem DashboardItem { get; set; } = null!;

        // A source is defined one of two ways, never both:
        //  - AnalyticId set: it points at an analytic saved on the tracker, and Code /
        //    ResultType / Fields are all null/empty.
        //  - AnalyticId null: it carries its own ad hoc definition in Code + ResultType +
        //    Fields. That definition only exists here, so it never shows up among the
        //    tracker's own analytics and disappears when the dashboard item is removed.
        public string? AnalyticId { get; set; }
        [ForeignKey(nameof(AnalyticId))]
        public virtual Analytic? Analytic { get; set; }

        public string? Code { get; set; }
        public string? ResultType { get; set; }
        public virtual List<DashboardItemSourceField> Fields { get; set; } = [];

        public string TrackerId { get; set; } = string.Empty;
        [ForeignKey(nameof(TrackerId))]
        public virtual Tracker Tracker { get; set; } = null!;

        [NotMapped]
        public bool IsAdHoc => string.IsNullOrEmpty(AnalyticId);
    }
}
