namespace Operum.Model.DTOs.Auth
{
    public class TokenResponseDto
    {
        public DateTime Expiry { get; set; }
        public string Token { get; set; } = string.Empty;
    }
}
