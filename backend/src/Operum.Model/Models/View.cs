using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    public class View
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;

        public string TrackerId { get; set; } = string.Empty;
        [ForeignKey(nameof(TrackerId))]
        public virtual Tracker Tracker { get; set; } = null!;

        //public virtual List<ViewFilter> Filters { get; set; } = [];
        public virtual List<ViewSort> Sorts { get; set; } = [];
        //public virtual List<ViewGroup> Groups { get; set; } = [];
        //public virtual List<ViewColumn> Columns { get; set; } = [];
    }
}
