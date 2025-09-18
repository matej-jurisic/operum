using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    public class Tracker
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Color { get; set; }

        public int? TrackerTypeId { get; set; } = null;
        [ForeignKey(nameof(TrackerTypeId))]
        public virtual TrackerType TrackerType { get; set; } = null!;

        public string OwnerId { get; set; } = string.Empty;
        [ForeignKey(nameof(OwnerId))]
        public virtual ApplicationUser Owner { get; set; } = null!;

        public string? DefaultViewId { get; set; }
        [ForeignKey(nameof(DefaultViewId))]
        public virtual View? DefaultView { get; set; }

        public virtual List<Field> Fields { get; set; } = [];
        public virtual List<View> Views { get; set; } = [];
    }
}
