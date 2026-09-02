using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Operum.API.Controllers.Base;
using Operum.API.Filters;
using Operum.Model.DTOs.Integrations.Requests;
using Operum.Service.Interfaces;

namespace Operum.API.Controllers
{
    [ApiController]
    [RequiresIntegrations]
    [Route("api/[controller]")]
    public class IntegrationsController(
        IIntegrationsService integrationsService,
        IIntegrationWebhookReceiver webhookReceiver) : BaseController
    {
        /// <summary>
        /// Where a push provider delivers. Anonymous by necessity -- the caller is the user's
        /// own Firefly instance, which has no Operum session. It authenticates with the
        /// unguessable token in the path plus a signature over the body, both checked below.
        /// </summary>
        [HttpPost("webhooks/{provider}/{token}")]
        [AllowAnonymous]
        // The one unauthenticated write in the app, so the body is capped well under anything
        // a real delivery needs.
        [RequestSizeLimit(1_000_000)]
        public async Task<IActionResult> ReceiveWebhook(
            [FromRoute] string provider, [FromRoute] string token, CancellationToken ct)
        {
            // Read as raw text, never through model binding: the signature covers the exact
            // bytes, and a re-serialized object would not hash the same.
            using var reader = new StreamReader(Request.Body);
            var rawBody = await reader.ReadToEndAsync(ct);

            var headers = Request.Headers.ToDictionary(
                h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase);

            return GetApiResponse(await webhookReceiver.Receive(provider, token, rawBody, headers, ct));
        }

        /// <summary>Installed providers and their source catalogs, for the mapping UI.</summary>
        [HttpGet("providers")]
        public IActionResult GetProviders()
        {
            return GetApiResponse(integrationsService.GetProviders());
        }

        [HttpGet]
        public async Task<IActionResult> GetIntegrations()
        {
            return GetApiResponse(await integrationsService.GetIntegrations());
        }

        [HttpPost]
        public async Task<IActionResult> Connect([FromBody] ConnectIntegrationDto dto)
        {
            return GetApiResponse(await integrationsService.Connect(dto));
        }

        [HttpDelete("{integrationId}")]
        public async Task<IActionResult> Disconnect([FromRoute] string integrationId)
        {
            return GetApiResponse(await integrationsService.Disconnect(integrationId));
        }

        [HttpPost("{integrationId}/targets")]
        public async Task<IActionResult> CreateTarget(
            [FromRoute] string integrationId, [FromBody] SaveIntegrationTargetDto dto)
        {
            return GetApiResponse(await integrationsService.CreateTarget(integrationId, dto));
        }

        [HttpPut("{integrationId}/targets/{targetId}")]
        public async Task<IActionResult> UpdateTarget(
            [FromRoute] string integrationId, [FromRoute] string targetId, [FromBody] SaveIntegrationTargetDto dto)
        {
            return GetApiResponse(await integrationsService.UpdateTarget(integrationId, targetId, dto));
        }

        [HttpDelete("{integrationId}/targets/{targetId}")]
        public async Task<IActionResult> DeleteTarget(
            [FromRoute] string integrationId, [FromRoute] string targetId)
        {
            return GetApiResponse(await integrationsService.DeleteTarget(integrationId, targetId));
        }

        [HttpPost("{integrationId}/targets/{targetId}/sync")]
        public async Task<IActionResult> SyncNow(
            [FromRoute] string integrationId, [FromRoute] string targetId)
        {
            return GetApiResponse(await integrationsService.SyncNow(integrationId, targetId));
        }

        /// <summary>
        /// Re-imports the target's whole history, overwriting the mapped fields on entries
        /// already imported. For picking up a field mapping added after the first import.
        /// </summary>
        [HttpPost("{integrationId}/targets/{targetId}/resync")]
        public async Task<IActionResult> ResyncTarget(
            [FromRoute] string integrationId, [FromRoute] string targetId)
        {
            return GetApiResponse(await integrationsService.ResyncTarget(integrationId, targetId));
        }

        /// <summary>
        /// Syncs every pull target on the connection at once, fetching each kind of data once
        /// and feeding all linked trackers from it -- rather than one provider call per target.
        /// </summary>
        [HttpPost("{integrationId}/sync")]
        public async Task<IActionResult> SyncIntegrationNow([FromRoute] string integrationId)
        {
            return GetApiResponse(await integrationsService.SyncIntegrationNow(integrationId));
        }

        /// <summary>
        /// Sets a push target's signing secret. Firefly III mints the secret in its own webhook
        /// screen, so the user pastes it here; for a provider Operum generates the secret for,
        /// an empty body issues a fresh one, returned once.
        /// </summary>
        [HttpPost("{integrationId}/targets/{targetId}/secret")]
        public async Task<IActionResult> SetWebhookSecret(
            [FromRoute] string integrationId, [FromRoute] string targetId, [FromBody] SetWebhookSecretDto dto)
        {
            return GetApiResponse(await integrationsService.SetWebhookSecret(integrationId, targetId, dto));
        }
    }
}
