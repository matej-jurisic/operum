using System.Text.Json.Serialization;

namespace Operum.Model.DTOs.Auth
{
    public class AuthResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string? UserName { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
        public string[] Roles { get; set; } = [];
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? TokenExpiry { get; set; }
        public string? Token { get; set; } = string.Empty;
    }
}
