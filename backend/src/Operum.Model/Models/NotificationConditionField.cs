using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    public class NotificationConditionField
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Purpose { get; set; } = string.Empty;

        public string ConditionId { get; set; } = string.Empty;
        [ForeignKey(nameof(ConditionId))]
        public virtual NotificationCondition Condition { get; set; } = null!;

        public string FieldId { get; set; } = string.Empty;
        [ForeignKey(nameof(FieldId))]
        public virtual Field Field { get; set; } = null!;
    }
}
