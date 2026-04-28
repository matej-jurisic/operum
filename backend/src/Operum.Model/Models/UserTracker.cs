using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    public class UserTracker
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;
        [ForeignKey(nameof(ApplicationUserId))]
        public virtual User ApplicationUser { get; set; } = null!;

        [Required]
        public string TrackerId { get; set; } = string.Empty;
        [ForeignKey(nameof(TrackerId))]
        public virtual Tracker Tracker { get; set; } = null!;

        public string? Type { get; set; }

        public bool CanEditData { get; set; }
        public bool CanEditSchema { get; set; }
        public int? Order { get; set; }
    }
}
