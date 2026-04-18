using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    public class TrackerConstantValueFilter
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string TrackerConstantValueId { get; set; } = string.Empty;
        public string FieldId { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public string? Value { get; set; }

        [ForeignKey(nameof(TrackerConstantValueId))]
        public virtual TrackerConstantValue TrackerConstantValue { get; set; } = null!;
    }
}
