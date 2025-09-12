namespace Operum.Model.DTOs.Users
{
    public class ApplicationUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string? UserName { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
        public string[] Roles { get; set; } = [];
        public bool? MailConfirmed { get; set; } = false;
    }
}
