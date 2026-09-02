using Operum.Model.Common;
using Operum.Model.DTOs.Integrations;
using Operum.Model.DTOs.Integrations.Requests;

namespace Operum.Service.Interfaces
{
    /// <summary>
    /// User-facing integration management. Everything here is scoped to the signed-in user;
    /// the sync loop uses <see cref="IIntegrationSyncExecutor"/> instead, which has no user.
    /// </summary>
    public interface IIntegrationsService
    {
        /// <summary>Installed providers and their source catalogs.</summary>
        Result<List<ProviderDto>> GetProviders();

        Task<Result<List<IntegrationDto>>> GetIntegrations();

        /// <summary>
        /// Verifies the credential with the provider before storing it, so a bad key is
        /// refused where it was typed rather than at the first sync.
        /// </summary>
        Task<Result<IntegrationDto>> Connect(ConnectIntegrationDto dto);

        /// <summary>
        /// Removes the connection and its targets. Entries already imported stay -- they are
        /// the user's data, not the integration's.
        /// </summary>
        Task<Result> Disconnect(string integrationId);

        Task<Result<IntegrationTargetDto>> CreateTarget(string integrationId, SaveIntegrationTargetDto dto);

        Task<Result<IntegrationTargetDto>> UpdateTarget(string integrationId, string targetId, SaveIntegrationTargetDto dto);

        Task<Result> DeleteTarget(string integrationId, string targetId);

        /// <summary>Runs a pull target now, through the same executor the scheduled tick uses.</summary>
        Task<Result<SyncResultDto>> SyncNow(string integrationId, string targetId);

        /// <summary>
        /// Re-imports a pull target's whole history: fetches every record from the backfill
        /// date again and overwrites the mapped fields on entries already imported. Used to
        /// populate a field mapping added after the first import; it discards any hand edits
        /// to those fields, so the UI confirms first.
        /// </summary>
        Task<Result<SyncResultDto>> ResyncTarget(string integrationId, string targetId);

        /// <summary>
        /// Runs every pull target on the connection now, fetching once per resource type
        /// rather than once per target. Same executor the scheduled tick uses.
        /// </summary>
        Task<Result<SyncResultDto>> SyncIntegrationNow(string integrationId);

        /// <summary>
        /// Issues a new webhook secret for a push target. The new value is in the response and
        /// nowhere else afterwards.
        /// </summary>
        Task<Result<IntegrationTargetDto>> RotateWebhookSecret(string integrationId, string targetId);
    }
}
