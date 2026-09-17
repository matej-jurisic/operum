using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    // Unlike ViewQuery there is no field binding: a DashboardView is tracker-agnostic, so
    // each following widget's link says which field the clause runs against.
    public class DashboardViewQuery
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string DashboardViewId { get; set; } = string.Empty;
        [ForeignKey(nameof(DashboardViewId))]
        public virtual DashboardView DashboardView { get; set; } = null!;

        public string QueryId { get; set; } = string.Empty;
        [ForeignKey(nameof(QueryId))]
        public virtual Query Query { get; set; } = null!;

        public int Order { get; set; }
    }
}
