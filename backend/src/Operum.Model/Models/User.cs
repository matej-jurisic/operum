using Microsoft.AspNetCore.Identity;

namespace Operum.Model.Models
{
    public class User : IdentityUser
    {
        public User() : base() { }
        public User(string email, string userName)
        {
            Email = email;
            UserName = userName;
        }

        public string? TimeZone { get; set; }
        public virtual List<RefreshToken> RefreshTokens { get; set; } = [];
    }
}
