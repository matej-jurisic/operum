using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    public class ViewColumn
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string ViewId { get; set; } = string.Empty;
        [ForeignKey(nameof(ViewId))]
        public virtual View View { get; set; } = null!;

        public string FieldId { get; set; } = string.Empty;
        public int Order { get; set; } = 0;
    }

}
