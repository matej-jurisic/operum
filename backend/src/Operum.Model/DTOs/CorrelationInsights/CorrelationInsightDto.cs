namespace Operum.Model.DTOs.CorrelationInsights
{
    public class CorrelationInsightDto
    {
        public string Id { get; set; } = string.Empty;

        public string TrackerAId { get; set; } = string.Empty;
        public string TrackerAName { get; set; } = string.Empty;
        public string? TrackerAColor { get; set; }
        public string MatchFieldAId { get; set; } = string.Empty;
        public string ValueFieldAId { get; set; } = string.Empty;
        public string ValueFieldAName { get; set; } = string.Empty;

        public string TrackerBId { get; set; } = string.Empty;
        public string TrackerBName { get; set; } = string.Empty;
        public string? TrackerBColor { get; set; }
        public string MatchFieldBId { get; set; } = string.Empty;
        public string ValueFieldBId { get; set; } = string.Empty;
        public string ValueFieldBName { get; set; } = string.Empty;

        public double Coefficient { get; set; }
        public int SampleSize { get; set; }
        public DateTime ComputedAt { get; set; }
    }
}
