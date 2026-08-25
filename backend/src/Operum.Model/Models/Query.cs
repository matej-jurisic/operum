using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    // A reusable, independently administrable filter+sort building block. Views are
    // composed of one or more Queries (see ViewQuery); a Query can be attached to
    // several Views at once.
    public class Query
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public string TrackerId { get; set; } = string.Empty;
        [ForeignKey(nameof(TrackerId))]
        public virtual Tracker Tracker { get; set; } = null!;

        public virtual List<QueryFilter> Filters { get; set; } = [];
        public virtual List<QuerySort> Sorts { get; set; } = [];
    }
}
