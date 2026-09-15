namespace Operum.Tests.Util
{
    /// <summary>
    /// A factory with a request limit low enough for a test to run into it.
    /// </summary>
    public class RateLimitedFactory : CustomWebApplicationFactory
    {
        public const int PermitLimit = 5;

        protected override IReadOnlyDictionary<string, string?> Settings => new Dictionary<string, string?>
        {
            ["RateLimiting:PermitLimit"] = PermitLimit.ToString(),
            ["RateLimiting:QueueLimit"] = "0",
        };
    }
}
