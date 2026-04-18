using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    public class TrackerConstantValue
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string TrackerConstantId { get; set; } = string.Empty;
        public int Priority { get; set; }
        public string Value { get; set; } = string.Empty;

        [ForeignKey(nameof(TrackerConstantId))]
        public virtual TrackerConstant TrackerConstant { get; set; } = null!;
        public virtual List<TrackerConstantValueFilter> Filters { get; set; } = [];
    }
}
