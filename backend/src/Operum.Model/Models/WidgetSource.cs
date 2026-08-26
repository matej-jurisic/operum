using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    // One tracker's contribution to a Widget: which tracker to read entries from and
    // which of its fields fill the purposes the widget's ResultType/Code require. Fixed
    // at creation -- the widget's definition is immutable, the same way an Analytic's
    // used to be -- so nothing here changes without creating a new widget.
    public class WidgetSource
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public int Order { get; set; }

        public string WidgetId { get; set; } = string.Empty;
        [ForeignKey(nameof(WidgetId))]
        public virtual Widget Widget { get; set; } = null!;

        public string TrackerId { get; set; } = string.Empty;
        [ForeignKey(nameof(TrackerId))]
        public virtual Tracker Tracker { get; set; } = null!;

        public virtual List<WidgetSourceField> Fields { get; set; } = [];
    }
}
