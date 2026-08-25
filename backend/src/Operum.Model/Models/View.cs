using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    public class View
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Order { get; set; }

        public string TrackerId { get; set; } = string.Empty;
        [ForeignKey(nameof(TrackerId))]
        public virtual Tracker Tracker { get; set; } = null!;

        public virtual List<ViewQuery> ViewQueries { get; set; } = [];

        // The fields this view shows, in the order it shows them. Empty means every field.
        public virtual List<ViewColumn> ViewColumns { get; set; } = [];
        //public virtual List<ViewGroup> Groups { get; set; } = [];
    }
}
