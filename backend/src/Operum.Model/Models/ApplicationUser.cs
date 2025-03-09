using Microsoft.AspNetCore.Identity;

namespace Operum.Model.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ApplicationUser() : base() { }
        public ApplicationUser(string email, string username)
        {
            Email = email;
            UserName = username;
        }
    }
}
