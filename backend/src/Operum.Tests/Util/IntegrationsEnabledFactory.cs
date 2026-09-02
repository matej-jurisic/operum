using Microsoft.Extensions.DependencyInjection;
using Operum.Service.Integrations.Intervals;
using Operum.Tests.Mocks;
using System.Net;

namespace Operum.Tests.Util
{
    /// <summary>
    /// The app with integrations switched on, and intervals.icu answered by a stub rather than
    /// the real service -- connecting validates a credential over HTTP, which a test must
    /// never actually do.
    /// </summary>
    public class IntegrationsEnabledFactory : CustomWebApplicationFactory
    {
        /// <summary>
        /// What the stubbed intervals.icu returns. Defaults to a valid athlete so the common
        /// case needs no setup; a test wanting a rejected key reassigns it before its first
        /// request.
        /// </summary>
        public (HttpStatusCode Status, string Body) IntervalsResponse { get; set; } =
            (HttpStatusCode.OK, """{ "id": "i123", "name": "Test Athlete" }""");

        public StubHttpMessageHandler IntervalsHandler { get; }

        public IntegrationsEnabledFactory() =>
            IntervalsHandler = new StubHttpMessageHandler(_ => IntervalsResponse);

        protected override IReadOnlyDictionary<string, string?> Settings => new Dictionary<string, string?>
        {
            ["Features:Integrations"] = "true",
            // Named in the webhook URLs the API hands back.
            ["ServerUrl"] = "https://operum.test",
        };

        protected override void ConfigureTestServices(IServiceCollection services)
        {
            services.AddHttpClient(IntervalsProvider.ProviderKey)
                .ConfigurePrimaryHttpMessageHandler(() => IntervalsHandler);
        }
    }
}
