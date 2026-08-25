using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    // A field a view shows, and where in the view's column order it sits.
    //
    // Columns hang off the view rather than off a Query on purpose: unlike a filter or a
    // sort there is no clause here, only a field, so there would be nothing to author once
    // and reuse across views. A view holding no ViewColumn at all shows every field.
    public class ViewColumn
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string ViewId { get; set; } = string.Empty;
        [ForeignKey(nameof(ViewId))]
        public virtual View View { get; set; } = null!;

        public string FieldId { get; set; } = string.Empty;
        [ForeignKey(nameof(FieldId))]
        public virtual Field Field { get; set; } = null!;

        public int Order { get; set; }
    }
}
