using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    public class FieldValue
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string EntryId { get; set; } = string.Empty;
        [ForeignKey(nameof(EntryId))]
        public virtual Entry Entry { get; set; } = null!;

        public string FieldId { get; set; } = string.Empty;
        [ForeignKey(nameof(FieldId))]
        public virtual Field Field { get; set; } = null!;

        public string? StringValue { get; set; }
        public double? NumberValue { get; set; }
        public DateTime? DateTimeValue { get; set; }
        public TimeSpan? TimeSpanValue { get; set; }
        public bool? BooleanValue { get; set; }
    }
}
