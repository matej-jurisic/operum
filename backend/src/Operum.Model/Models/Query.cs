using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Operum.Model.Constants;

namespace Operum.Model.Models
{
    // A reusable, field-agnostic clause (filter or sort) bound to a concrete field at the
    // point of use -- ViewQuery.FieldId for a tracker view, or a per-widget map on a
    // dashboard. Rows are value-deduplicated per owner (see QueryPool); a Query has no
    // name and is never surfaced to the client on its own.
    public class Query
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // QueryKinds.Filter or QueryKinds.Sort.
        public string Kind { get; set; } = QueryKinds.Filter;

        public string OwnerId { get; set; } = string.Empty;
        [ForeignKey(nameof(OwnerId))]
        public virtual User Owner { get; set; } = null!;

        // A Constants.Fields.DataTypes value. Whatever field this clause is later bound to
        // must be of this type.
        public string DataType { get; set; } = string.Empty;

        // Filters only. A null Value means "has no value".
        public string? Operator { get; set; }
        public string? Value { get; set; }

        // Sorts only.
        public bool Descending { get; set; }
    }
}
