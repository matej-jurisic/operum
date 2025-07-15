namespace Operum.Model.DTOs.Requests
{
    public class LoginRequestDto
    {
        public string Credentials { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
