using Operum.Model.Common;
using Operum.Model.DTOs.Integrations;

namespace Operum.Service.Interfaces
{
    // Runs with no signed-in user: the delivery authenticates via an unguessable path token plus a body signature.
    public interface IIntegrationWebhookReceiver
    {
        // rawBody must be the delivery's exact bytes as text; signatures are computed over these.
        Task<Result<SyncResultDto>> Receive(
            string providerKey,
            string token,
            string rawBody,
            IReadOnlyDictionary<string, string> headers,
            CancellationToken ct = default);
    }
}
