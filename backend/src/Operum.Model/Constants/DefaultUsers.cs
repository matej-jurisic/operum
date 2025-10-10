using Operum.Model.DTOs.Auth.Requests;

namespace Operum.Model.Constants
{
    public static class DefaultUsers
    {
        public readonly static RegisterDto AdminUserData = new()
        {
            Email = "admin@example.com",
            Password = "Password0!",
            UserName = "admin",
        };

        public readonly static RegisterDto TestUserData = new()
        {
            Email = "test@example.com",
            Password = "Password0!",
            UserName = "test",
        };
    }
}
