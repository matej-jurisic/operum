using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    public class Field
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Type { get; set; } = string.Empty;
        public bool Required { get; set; } = false;
        public bool Visible { get; set; } = true;
        public int Order { get; set; }
        public string? SelectOptions { get; set; }
        public bool IsCalculated { get; set; } = false;
        public string? Formula { get; set; }

        public string TrackerId { get; set; } = string.Empty;
        [ForeignKey(nameof(TrackerId))]
        public virtual Tracker Tracker { get; set; } = null!;

        public virtual List<FieldValue> FieldValues { get; set; } = [];
        public virtual List<AnalyticField> AnalyticFields { get; set; } = [];
    }
}
