using Microsoft.AspNetCore.Identity;

namespace Operum.Model.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ApplicationUser() : base() { }
        public ApplicationUser(string email, string userName)
        {
            Email = email;
            UserName = userName;
        }
    }
}
