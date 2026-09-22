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

        // Reference fields only. Nulled when the target tracker is deleted, leaving the field
        // degraded (read-only, values keep their last cached label).
        public string? ReferencedTrackerId { get; set; }
        [ForeignKey(nameof(ReferencedTrackerId))]
        public virtual Tracker? ReferencedTracker { get; set; }

        // Reference fields only: shown as the link label and cached into FieldValue.StringValue.
        // Null falls back to the target entry's creation date.
        public string? ReferencedDisplayFieldId { get; set; }
        [ForeignKey(nameof(ReferencedDisplayFieldId))]
        public virtual Field? ReferencedDisplayField { get; set; }

        // Mutually exclusive with DefaultValueConstantId: a literal (or, for date/datetime, a
        // DynamicDateTokens token) applied when a new entry is created.
        public string? DefaultValue { get; set; }

        // Mutually exclusive with DefaultValue. Nulled when the constant is deleted, leaving the
        // field with no default instead of destroying the schema around it.
        public string? DefaultValueConstantId { get; set; }
        [ForeignKey(nameof(DefaultValueConstantId))]
        public virtual TrackerConstant? DefaultValueConstant { get; set; }

        // When set, the field is hidden from the create-entry form unless VisibilityFieldId's live
        // value matches VisibilityOperator/VisibilityValue. Nulled when the target field is
        // deleted, leaving the field always visible instead of destroying the schema around it.
        public string? VisibilityFieldId { get; set; }
        [ForeignKey(nameof(VisibilityFieldId))]
        public virtual Field? VisibilityField { get; set; }
        public string? VisibilityOperator { get; set; }
        public string? VisibilityValue { get; set; }

        public string TrackerId { get; set; } = string.Empty;
        [ForeignKey(nameof(TrackerId))]
        public virtual Tracker Tracker { get; set; } = null!;

        public virtual List<FieldValue> FieldValues { get; set; } = [];
    }
}
