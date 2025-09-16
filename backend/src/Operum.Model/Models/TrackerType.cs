using System.ComponentModel.DataAnnotations;

namespace Operum.Model.Models
{
    public class TrackerType
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
