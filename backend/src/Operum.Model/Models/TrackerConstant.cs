using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    public class TrackerConstant
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;

        public string TrackerId { get; set; } = string.Empty;
        [ForeignKey(nameof(TrackerId))]
        public virtual Tracker Tracker { get; set; } = null!;
        public virtual List<TrackerConstantValue> Values { get; set; } = [];
    }
}
