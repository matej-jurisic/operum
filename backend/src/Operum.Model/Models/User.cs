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

        /// The route the app opens on after load, e.g. "/dashboard" or "/trackers/{id}".
        /// Null means fall back to the default dashboard.
        public string? DefaultPage { get; set; }

        public virtual List<RefreshToken> RefreshTokens { get; set; } = [];
    }
}
