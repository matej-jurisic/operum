using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    public class AnalyticRequiredDataType
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string AnalyticId { get; set; } = string.Empty;
        [ForeignKey(nameof(AnalyticId))]
        public virtual Analytic Analytic { get; set; } = null!;

        public string Type { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
    }
}
