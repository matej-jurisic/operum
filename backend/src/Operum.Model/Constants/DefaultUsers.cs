using Operum.Model.DTOs.Requests;

namespace Operum.Model.Constants
{
    public static class DefaultUsers
    {
        public readonly static RegisterRequestDto AdminUserData = new()
        {
            Email = "admin@example.com",
            Password = "Password0!",
            UserName = "admin",
        };

        public readonly static RegisterRequestDto TestUserData = new()
        {
            Email = "test@example.com",
            Password = "Password0!",
            UserName = "test",
        };
    }
}
