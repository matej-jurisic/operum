namespace Operum.Model.DTOs.Dashboard
{
    // QueryId is the pooled clause's id (see QueryPool), keyed by a filter widget's FieldByQuery map.
    public class DashboardViewDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
        public List<DashboardViewClauseDto> Clauses { get; set; } = [];
    }

    public class DashboardViewClauseDto
    {
        public string QueryId { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string? Operator { get; set; }
        public string? Value { get; set; }
        public bool Descending { get; set; }
    }
}
