using Operum.Model.Models;

namespace Operum.Service.Domain.Analytics
{
    public class AnalyticResultBuilderRequest
    {
        public Analytic Analytic { get; set; } = null!;
        public IEnumerable<Entry> Entries { get; set; } = null!;
        public Dictionary<string, Field> FieldMap { get; set; } = null!;
    }
}
