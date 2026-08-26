using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    // Purpose -> Field mapping for a widget source: the equivalent of the old
    // AnalyticField/DashboardItemSourceField now that both have one shared home.
    public class WidgetSourceField
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Purpose { get; set; } = string.Empty;

        public string WidgetSourceId { get; set; } = string.Empty;
        [ForeignKey(nameof(WidgetSourceId))]
        public virtual WidgetSource WidgetSource { get; set; } = null!;

        public string FieldId { get; set; } = string.Empty;
        [ForeignKey(nameof(FieldId))]
        public virtual Field Field { get; set; } = null!;
    }
}
