using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    public class TrackerAnalytic
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public string TrackerId { get; set; } = string.Empty;
        [ForeignKey(nameof(TrackerId))]
        public virtual Tracker Tracker { get; set; } = null!;

        public string AnalyticId { get; set; } = string.Empty;
        [ForeignKey(nameof(AnalyticId))]
        public virtual Analytic Analytic { get; set; } = null!;

        public virtual List<TrackerAnalyticField> TrackerAnalyticFields { get; set; } = [];
    }
}
