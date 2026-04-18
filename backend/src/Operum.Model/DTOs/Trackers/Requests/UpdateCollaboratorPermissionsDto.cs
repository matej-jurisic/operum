namespace Operum.Model.DTOs.Trackers.Requests
{
    public class UpdateCollaboratorPermissionsDto
    {
        public required string Username { get; set; } = string.Empty;
        public bool CanEditData { get; set; }
        public bool CanEditSchema { get; set; }
    }
}
