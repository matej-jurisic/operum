using Operum.Model.Common;
using Operum.Model.DTOs.Integrations;

namespace Operum.Service.Interfaces
{
    /// <summary>
    /// Takes a webhook delivery and applies it. Runs with no signed-in user: the delivery
    /// authenticates itself with an unguessable path token plus a signature over its body, and
    /// nothing here trusts anything else about the request.
    /// </summary>
    public interface IIntegrationWebhookReceiver
    {
        /// <param name="rawBody">
        /// The delivery's exact bytes as text. Signatures are computed over these, so a
        /// re-serialized object will not verify.
        /// </param>
        Task<Result<SyncResultDto>> Receive(
            string providerKey,
            string token,
            string rawBody,
            IReadOnlyDictionary<string, string> headers,
            CancellationToken ct = default);
    }
}
