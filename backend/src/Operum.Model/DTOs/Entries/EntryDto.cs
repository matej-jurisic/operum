using Operum.Model.DTOs.FieldValue;

namespace Operum.Model.DTOs.Entry
{
    public class EntryDto
    {
        public string Id { get; set; } = string.Empty;
        public string TrackerId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public ICollection<FieldValueDto> FieldValues { get; set; } = [];
    }
}
