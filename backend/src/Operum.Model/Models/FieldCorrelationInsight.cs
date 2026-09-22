using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    // A discovered statistical correlation between a Number/TimeSpan field in one tracker and
    // one in another, joined on each tracker's date field (see CorrelationInsightEvaluator).
    // Recomputed in place on every run; Dismissed survives a recompute so a user's "not
    // interested" sticks until the pair itself stops qualifying.
    public class FieldCorrelationInsight
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string UserId { get; set; } = string.Empty;
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;

        public string TrackerAId { get; set; } = string.Empty;
        [ForeignKey(nameof(TrackerAId))]
        public virtual Tracker TrackerA { get; set; } = null!;

        public string TrackerBId { get; set; } = string.Empty;
        [ForeignKey(nameof(TrackerBId))]
        public virtual Tracker TrackerB { get; set; } = null!;

        // Each tracker's own date field, used to join its entries with the other side's.
        public string MatchFieldAId { get; set; } = string.Empty;
        [ForeignKey(nameof(MatchFieldAId))]
        public virtual Field MatchFieldA { get; set; } = null!;

        public string MatchFieldBId { get; set; } = string.Empty;
        [ForeignKey(nameof(MatchFieldBId))]
        public virtual Field MatchFieldB { get; set; } = null!;

        public string ValueFieldAId { get; set; } = string.Empty;
        [ForeignKey(nameof(ValueFieldAId))]
        public virtual Field ValueFieldA { get; set; } = null!;

        public string ValueFieldBId { get; set; } = string.Empty;
        [ForeignKey(nameof(ValueFieldBId))]
        public virtual Field ValueFieldB { get; set; } = null!;

        // Pearson coefficient, -1..1.
        public double Coefficient { get; set; }
        public int SampleSize { get; set; }
        public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
        public bool Dismissed { get; set; } = false;
    }
}
