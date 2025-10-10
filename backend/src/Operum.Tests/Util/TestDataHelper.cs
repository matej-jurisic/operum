using Operum.Model.DTOs.Auth.Requests;

namespace Operum.Tests.Util
{
    public static class TestDataHelper
    {
        public static RegisterDto CreateUniqueRegisterPayload()
        {
            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            return new RegisterDto()
            {
                UserName = $"test_{uniqueId}",
                Email = $"test_{uniqueId}@example.com",
                Password = "MyStrongPassword123!"
            };
        }
    }
}
