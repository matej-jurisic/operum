using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    /// <summary>
    /// One value from the provider wired to one field on the tracker. Projects onto
    /// <c>Operum.Model.Integrations.FieldMapping</c>, which is what the projector and the
    /// validator actually work with -- neither of them needs a database.
    /// </summary>
    public class IntegrationFieldMapping
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>A <c>SourceField.Key</c> from the provider's catalog for this resource.</summary>
        public string SourceKey { get; set; } = string.Empty;

        /// <summary>
        /// Whether a value the provider reports as empty leaves the field alone (true) or
        /// clears it (false). See MappingValidator: true is refused on a required field.
        /// </summary>
        public bool SkipWhenNull { get; set; } = true;

        public string TargetId { get; set; } = string.Empty;
        [ForeignKey(nameof(TargetId))]
        public virtual IntegrationTarget Target { get; set; } = null!;

        public string FieldId { get; set; } = string.Empty;
        [ForeignKey(nameof(FieldId))]
        public virtual Field Field { get; set; } = null!;
    }
}
