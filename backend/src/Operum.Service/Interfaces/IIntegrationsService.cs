using Operum.Model.Common;
using Operum.Model.DTOs.Integrations;
using Operum.Model.DTOs.Integrations.Requests;

namespace Operum.Service.Interfaces
{
    // Scoped to the signed-in user; the sync loop uses IIntegrationSyncExecutor instead, which has no user.
    public interface IIntegrationsService
    {
        Result<List<ProviderDto>> GetProviders();

        Task<Result<List<IntegrationDto>>> GetIntegrations();

        Task<Result<IntegrationDto>> Connect(ConnectIntegrationDto dto);

        // Entries already imported stay: they are the user's data, not the integration's.
        Task<Result> Disconnect(string integrationId);

        Task<Result<IntegrationTargetDto>> CreateTarget(string integrationId, SaveIntegrationTargetDto dto);

        Task<Result<IntegrationTargetDto>> UpdateTarget(string integrationId, string targetId, SaveIntegrationTargetDto dto);

        Task<Result> DeleteTarget(string integrationId, string targetId);

        Task<Result<SyncResultDto>> SyncNow(string integrationId, string targetId);

        // Re-fetches the whole history and overwrites mapped fields on entries already imported,
        // discarding any hand edits to them.
        Task<Result<SyncResultDto>> ResyncTarget(string integrationId, string targetId);

        // Fetches once per resource type rather than once per target.
        Task<Result<SyncResultDto>> SyncIntegrationNow(string integrationId);

        // For a provider that mints its own secret (Firefly III) this stores the pasted value;
        // otherwise passing no secret issues a fresh one, returned only in this response.
        Task<Result<IntegrationTargetDto>> SetWebhookSecret(string integrationId, string targetId, SetWebhookSecretDto dto);
    }
}
