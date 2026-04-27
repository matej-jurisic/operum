namespace Operum.Model.DTOs.Admin
{
    public class AdminTrackerDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Color { get; set; }
        public string OwnerId { get; set; } = string.Empty;
        public string? OwnerName { get; set; }
        public int FieldCount { get; set; }
        public int EntryCount { get; set; }
        public int CollaboratorCount { get; set; }
    }
}
