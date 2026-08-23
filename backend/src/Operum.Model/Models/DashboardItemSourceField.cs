using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    // Purpose -> Field mapping for an ad hoc dashboard source, i.e. the equivalent of
    // AnalyticField but owned by the source rather than by a persisted Analytic, so it
    // is created and deleted together with the dashboard item it belongs to.
    public class DashboardItemSourceField
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Purpose { get; set; } = string.Empty;

        public string DashboardItemSourceId { get; set; } = string.Empty;
        [ForeignKey(nameof(DashboardItemSourceId))]
        public virtual DashboardItemSource DashboardItemSource { get; set; } = null!;

        public string FieldId { get; set; } = string.Empty;
        [ForeignKey(nameof(FieldId))]
        public virtual Field Field { get; set; } = null!;
    }
}
