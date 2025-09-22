using Operum.Model.DTOs.Fields;

namespace Operum.Model.DTOs.Trackers
{
    public class TrackerDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Color { get; set; }
        public int? TrackerTypeId { get; set; } = null;
        public string? TrackerTypeName { get; set; }
        public string? DefaultViewId { get; set; }
        public string? OwnerId { get; set; } = string.Empty;
        public string? OwnerName { get; set; } = string.Empty;
        public List<FieldDto> Fields { get; set; } = [];
    }
}
