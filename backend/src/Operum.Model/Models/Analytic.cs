using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    public class Analytic
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }

        public int AnalyticTypeId { get; set; }
        [ForeignKey(nameof(AnalyticTypeId))]
        public virtual AnalyticType AnalyticType { get; set; } = null!;

        public virtual List<AnalyticRequiredDataType> AnalyticRequiredDataTypes { get; set; } = [];
    }
}
