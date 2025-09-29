using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    public class TrackerAnalyticField
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string TrackerAnalyticId { get; set; } = string.Empty;
        [ForeignKey(nameof(TrackerAnalyticId))]
        public virtual TrackerAnalytic TrackerAnalytic { get; set; } = null!;

        public string FieldId { get; set; } = string.Empty;
        [ForeignKey(nameof(FieldId))]
        public virtual Field Field { get; set; } = null!;

        public string AnalyticRequiredDataTypeId { get; set; } = string.Empty;
        [ForeignKey(nameof(AnalyticRequiredDataTypeId))]
        public virtual AnalyticRequiredDataType AnalyticRequiredDataType { get; set; } = null!;
    }
}
