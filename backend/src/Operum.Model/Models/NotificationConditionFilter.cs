using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    public class NotificationConditionFilter
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? FieldId { get; set; }
        [ForeignKey(nameof(FieldId))]
        public virtual Field? Field { get; set; }

        public string Operator { get; set; } = string.Empty;
        public string? Value { get; set; }

        public string ConditionId { get; set; } = string.Empty;
        [ForeignKey(nameof(ConditionId))]
        public virtual NotificationCondition Condition { get; set; } = null!;
    }
}
