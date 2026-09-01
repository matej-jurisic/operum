namespace Operum.Model.DTOs.Dashboard
{
    // A named clause set a dashboard's view selectors can offer. Clauses are field-agnostic:
    // QueryId is the pooled clause's id, which is how a view selector's FieldByQuery map
    // keys the per-widget field it runs against.
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
