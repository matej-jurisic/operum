using System.ComponentModel.DataAnnotations;

namespace Operum.Model.DTOs.Auth.Requests
{
    public class GoogleLoginDto
    {
        [Required]
        public string Credential { get; set; } = string.Empty;
    }
}
