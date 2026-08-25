using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Operum.Model.Constants;

namespace Operum.Model.Models
{
    // A reusable, independently administrable single clause: one filter or one sort over a
    // field of its tracker. Views are composed of one or more Queries (see ViewQuery); a
    // Query can be attached to several Views at once. It has no name of its own - what it
    // does is read off the field, operator and value.
    //
    // Which columns a view shows is not a clause and is not stored here (see ViewColumn).
    public class Query
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // QueryKinds.Filter or QueryKinds.Sort.
        public string Kind { get; set; } = QueryKinds.Filter;

        public string TrackerId { get; set; } = string.Empty;
        [ForeignKey(nameof(TrackerId))]
        public virtual Tracker Tracker { get; set; } = null!;

        public string FieldId { get; set; } = string.Empty;
        [ForeignKey(nameof(FieldId))]
        public virtual Field Field { get; set; } = null!;

        // Filters only. A null Value means "has no value".
        public string? Operator { get; set; }
        public string? Value { get; set; }

        // Sorts only.
        public bool Descending { get; set; }
    }
}
