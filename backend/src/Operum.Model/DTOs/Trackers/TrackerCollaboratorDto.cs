namespace Operum.Model.DTOs.Trackers
{
    public class TrackerCollaboratorDto
    {
        public string Id { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public bool CanEditData { get; set; }
        public bool CanEditSchema { get; set; }
    }
}
