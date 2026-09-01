using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    // Attaches a Query to a View and binds its field-agnostic clause to one concrete field
    // of the view's tracker. Order decides both display order and sort-merge precedence
    // within the view (first-field-wins across queries, in Order).
    public class ViewQuery
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string ViewId { get; set; } = string.Empty;
        [ForeignKey(nameof(ViewId))]
        public virtual View View { get; set; } = null!;

        public string QueryId { get; set; } = string.Empty;
        [ForeignKey(nameof(QueryId))]
        public virtual Query Query { get; set; } = null!;

        // Which field of the view's tracker this clause runs against. Its Type must match
        // Query.DataType. Deleting the field drops this row (see OperumContext).
        public string FieldId { get; set; } = string.Empty;
        [ForeignKey(nameof(FieldId))]
        public virtual Field Field { get; set; } = null!;

        public int Order { get; set; }
    }
}
