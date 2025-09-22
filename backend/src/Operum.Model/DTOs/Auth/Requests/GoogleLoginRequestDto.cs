using System.ComponentModel.DataAnnotations;

namespace Operum.Model.DTOs.Auth.Requests
{
    public class GoogleLoginRequestDto
    {
        [Required]
        public string Credential { get; set; } = string.Empty;
    }
}
