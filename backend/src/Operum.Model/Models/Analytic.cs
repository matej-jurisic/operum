using System.ComponentModel.DataAnnotations;

namespace Operum.Model.Models
{
    public class Analytic
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public virtual List<AnalyticRequiredDataType> AnalyticRequiredDataTypes { get; set; } = [];
    }
}
