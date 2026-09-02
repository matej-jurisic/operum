using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.DTOs.Integrations;
using Operum.Model.DTOs.Integrations.Requests;
using Operum.Model.Enums;
using Operum.Model.Integrations;
using Operum.Model.Models;
using Operum.Service.Domain.Integrations;
using Operum.Service.Integrations;
using Operum.Service.Interfaces;
using System.Security.Cryptography;

namespace Operum.Service.Services.Integrations
{
    public class IntegrationsService(
        OperumContext db,
        ICurrentUserService currentUserService,
        IIntegrationProviderRegistry registry,
        ICredentialProtector credentialProtector,
        IIntegrationSyncExecutor syncExecutor,
        IConfiguration configuration) : IIntegrationsService
    {
        public Result<List<ProviderDto>> GetProviders()
        {
            var providers = registry.All.Select(provider => new ProviderDto
            {
                Key = provider.Key,
                DisplayName = provider.DisplayName,
                SupportsPull = provider.Capabilities.HasFlag(IntegrationCapabilities.Pull),
                SupportsPush = provider.Capabilities.HasFlag(IntegrationCapabilities.Push),
                RequiresBaseUrl = provider.RequiresBaseUrl,
                Resources = [.. provider.ResourceTypes.Select(resourceType => new ProviderResourceDto
                {
                    ResourceType = resourceType,
                    Fields = [.. provider.Catalog(resourceType).Select(field => new SourceFieldDto
                    {
                        Key = field.Key,
                        Type = field.Type,
                        Label = field.Label,
                        Description = field.Description,
                    })],
                })],
            }).ToList();

            return Result.Success(providers);
        }

        public async Task<Result<List<IntegrationDto>>> GetIntegrations()
        {
            var userId = currentUserService.GetCurrentUser().Id;

            var integrations = await db.Integrations
                .Include(i => i.Targets)
                    .ThenInclude(t => t.Mappings)
                .Include(i => i.Targets)
                    .ThenInclude(t => t.Tracker)
                .Where(i => i.UserId == userId)
                .OrderBy(i => i.CreatedAt)
                .ToListAsync();

            return Result.Success(integrations.Select(ToDto).ToList());
        }

        public async Task<Result<IntegrationDto>> Connect(ConnectIntegrationDto dto)
        {
            var userId = currentUserService.GetCurrentUser().Id;

            var provider = registry.Get(dto.Provider);
            if (provider == null)
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("provider"));

            var count = await db.Integrations.CountAsync(i => i.UserId == userId);
            if (count >= DataLimits.MaxIntegrationCount)
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.MaxNumberReached("integrations", DataLimits.MaxIntegrationCount));

            if (provider.RequiresBaseUrl && string.IsNullOrWhiteSpace(dto.BaseUrl))
                return Result.Failure(ResultStatusCodes.BadRequest, $"{provider.DisplayName} is self-hosted, so it needs the address of your instance.");

            var baseUrlError = ValidateBaseUrl(dto.BaseUrl);
            if (baseUrlError != null)
                return Result.Failure(ResultStatusCodes.BadRequest, baseUrlError);

            string? externalAccountId = null;

            // A pull provider can prove the credential before anything is stored. A push-only
            // provider has nothing to call, so its connection is made unverified and the first
            // delivery is what proves it.
            if (registry.GetPull(provider.Key) is { } pullProvider)
            {
                var connection = new ProviderConnection(dto.BaseUrl, dto.Credential, null);
                var validation = await pullProvider.ValidateCredentialAsync(connection);

                if (validation.IsFailure)
                    return Result.Failure(validation.StatusCode, validation.Messages);

                externalAccountId = validation.Data.ExternalAccountId;
            }

            var duplicate = await db.Integrations.AnyAsync(i =>
                i.UserId == userId && i.Provider == provider.Key && i.ExternalAccountId == externalAccountId);

            if (duplicate)
                return Result.Failure(ResultStatusCodes.Conflict, "That account is already connected.");

            var integration = new Integration
            {
                UserId = userId,
                Provider = provider.Key,
                ExternalAccountId = externalAccountId,
                BaseUrl = string.IsNullOrWhiteSpace(dto.BaseUrl) ? null : dto.BaseUrl.Trim(),
                CredentialCiphertext = string.IsNullOrWhiteSpace(dto.Credential)
                    ? null
                    : credentialProtector.Protect(dto.Credential),
            };

            db.Integrations.Add(integration);
            await db.SaveChangesAsync();

            return Result.Success(ToDto(integration), Messages.Success);
        }

        public async Task<Result> Disconnect(string integrationId)
        {
            var integration = await LoadOwned(integrationId);
            if (integration == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("integration"));

            // Targets and mappings cascade. Entries already imported keep their Source and
            // ExternalId and are otherwise untouched -- they are the user's data.
            db.Integrations.Remove(integration);
            await db.SaveChangesAsync();

            return Result.Success(Messages.Success);
        }

        public async Task<Result<IntegrationTargetDto>> CreateTarget(string integrationId, SaveIntegrationTargetDto dto)
        {
            var integration = await LoadOwned(integrationId);
            if (integration == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("integration"));

            if (integration.Targets.Count >= DataLimits.MaxIntegrationTargetCount)
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.MaxNumberReached("integration targets", DataLimits.MaxIntegrationTargetCount));

            var prepared = await Prepare(integration, dto);
            if (prepared.IsFailure)
                return Result.Failure(prepared.StatusCode, prepared.Messages);

            var (provider, mode, fields) = prepared.Data;

            var duplicate = integration.Targets.Any(t =>
                t.TrackerId == dto.TrackerId && t.ResourceType == dto.ResourceType);

            if (duplicate)
                return Result.Failure(ResultStatusCodes.Conflict, "This connection already feeds that tracker with this data.");

            var target = new IntegrationTarget
            {
                IntegrationId = integration.Id,
                TrackerId = dto.TrackerId,
                ResourceType = dto.ResourceType,
                Mode = mode,
                IsEnabled = dto.IsEnabled,
                BackfillFrom = dto.BackfillFrom ?? DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
                Mappings = [.. dto.Mappings.Select(m => new IntegrationFieldMapping
                {
                    SourceKey = m.SourceKey,
                    FieldId = m.FieldId,
                    SkipWhenNull = m.SkipWhenNull,
                })],
            };

            string? plaintextSecret = null;
            if (mode == IntegrationMode.Push)
            {
                plaintextSecret = NewSecret();
                target.WebhookToken = NewSecret();
                target.WebhookSecretCiphertext = credentialProtector.Protect(plaintextSecret);
            }

            db.IntegrationTargets.Add(target);
            await db.SaveChangesAsync();

            // Reloaded so the DTO can name the tracker without a second round trip in the UI.
            target.Tracker = await db.Trackers.FirstAsync(t => t.Id == target.TrackerId);

            // The only response that ever carries the secret.
            return Result.Success(ToDto(target, integration.Provider, plaintextSecret), Messages.Success);
        }

        public async Task<Result<IntegrationTargetDto>> UpdateTarget(string integrationId, string targetId, SaveIntegrationTargetDto dto)
        {
            var integration = await LoadOwned(integrationId);
            if (integration == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("integration"));

            var target = integration.Targets.FirstOrDefault(t => t.Id == targetId);
            if (target == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("integration target"));

            // Which tracker and which resource a target feeds are what identify it; changing
            // them would orphan whatever it already imported under the old pairing. Delete and
            // remake instead.
            if (target.TrackerId != dto.TrackerId || target.ResourceType != dto.ResourceType)
                return Result.Failure(ResultStatusCodes.BadRequest, "A target's tracker and data type cannot be changed. Remove it and add a new one.");

            var prepared = await Prepare(integration, dto);
            if (prepared.IsFailure)
                return Result.Failure(prepared.StatusCode, prepared.Messages);

            target.IsEnabled = dto.IsEnabled;

            if (dto.BackfillFrom != null)
                target.BackfillFrom = dto.BackfillFrom.Value;

            db.IntegrationFieldMappings.RemoveRange(target.Mappings);
            target.Mappings = [.. dto.Mappings.Select(m => new IntegrationFieldMapping
            {
                TargetId = target.Id,
                SourceKey = m.SourceKey,
                FieldId = m.FieldId,
                SkipWhenNull = m.SkipWhenNull,
            })];

            await db.SaveChangesAsync();

            return Result.Success(ToDto(target, integration.Provider), Messages.Success);
        }

        public async Task<Result> DeleteTarget(string integrationId, string targetId)
        {
            var integration = await LoadOwned(integrationId);
            if (integration == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("integration"));

            var target = integration.Targets.FirstOrDefault(t => t.Id == targetId);
            if (target == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("integration target"));

            db.IntegrationTargets.Remove(target);
            await db.SaveChangesAsync();

            return Result.Success(Messages.Success);
        }

        public async Task<Result<SyncResultDto>> SyncNow(string integrationId, string targetId)
        {
            var integration = await LoadOwned(integrationId);
            if (integration == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("integration"));

            var target = integration.Targets.FirstOrDefault(t => t.Id == targetId);
            if (target == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("integration target"));

            // Same executor the scheduled tick runs, so a manual sync cannot behave differently
            // from an automatic one.
            var result = await syncExecutor.SyncTargetAsync(target.Id);
            if (result.IsFailure)
                return Result.Failure(result.StatusCode, result.Messages);

            return Result.Success(ToDto(result.Data), Messages.Success);
        }

        public async Task<Result<SyncResultDto>> ResyncTarget(string integrationId, string targetId)
        {
            var integration = await LoadOwned(integrationId);
            if (integration == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("integration"));

            var target = integration.Targets.FirstOrDefault(t => t.Id == targetId);
            if (target == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("integration target"));

            // Same executor as a normal sync, told to ignore the cursor for this run so it
            // re-reads and re-applies the whole history rather than just what changed.
            var result = await syncExecutor.SyncTargetAsync(target.Id, fullResync: true);
            if (result.IsFailure)
                return Result.Failure(result.StatusCode, result.Messages);

            return Result.Success(ToDto(result.Data), Messages.Success);
        }

        public async Task<Result<SyncResultDto>> SyncIntegrationNow(string integrationId)
        {
            var integration = await LoadOwned(integrationId);
            if (integration == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("integration"));

            var result = await syncExecutor.SyncIntegrationAsync(integration.Id);
            if (result.IsFailure)
                return Result.Failure(result.StatusCode, result.Messages);

            return Result.Success(ToDto(result.Data), Messages.Success);
        }

        private static SyncResultDto ToDto(EntryWriteResult written) => new()
        {
            Created = written.Created,
            Updated = written.Updated,
            Deleted = written.Deleted,
            Skipped = written.Skipped,
            ErrorCount = written.ErrorCount,
            Errors = written.Errors,
        };

        public async Task<Result<IntegrationTargetDto>> RotateWebhookSecret(string integrationId, string targetId)
        {
            var integration = await LoadOwned(integrationId);
            if (integration == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("integration"));

            var target = integration.Targets.FirstOrDefault(t => t.Id == targetId);
            if (target == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("integration target"));

            if (target.Mode != IntegrationMode.Push)
                return Result.Failure(ResultStatusCodes.BadRequest, "Only a webhook target has a secret.");

            var plaintextSecret = NewSecret();
            target.WebhookSecretCiphertext = credentialProtector.Protect(plaintextSecret);
            await db.SaveChangesAsync();

            return Result.Success(ToDto(target, integration.Provider, plaintextSecret), Messages.Success);
        }

        // ---- shared checks ----

        /// <summary>
        /// The validation a create and an update both need: the provider serves this resource,
        /// the caller owns the tracker, the mode is one the provider supports, and every
        /// mapping stands up against the catalog and the tracker's fields.
        /// </summary>
        private async Task<Result<(IIntegrationProvider Provider, IntegrationMode Mode, List<Field> Fields)>> Prepare(
            Integration integration, SaveIntegrationTargetDto dto)
        {
            var userId = currentUserService.GetCurrentUser().Id;

            var provider = registry.Get(integration.Provider);
            if (provider == null)
                return Result.Failure(ResultStatusCodes.BadRequest, $"No integration provider is installed for '{integration.Provider}'.");

            if (!provider.ResourceTypes.Contains(dto.ResourceType))
                return Result.Failure(ResultStatusCodes.BadRequest, $"{provider.DisplayName} does not provide '{dto.ResourceType}'.");

            var tracker = await db.Trackers.FirstOrDefaultAsync(t => t.Id == dto.TrackerId);
            if (tracker == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("tracker"));

            // Owner-only, matching how tracker metadata and collaborator management already
            // work. A collaborator with CanEditData cannot attach their own connection to
            // someone else's tracker.
            if (tracker.OwnerId != userId)
                return Result.Failure(ResultStatusCodes.Forbidden);

            var modeResult = ResolveMode(dto.Mode, provider);
            if (modeResult.IsFailure)
                return Result.Failure(modeResult.StatusCode, modeResult.Messages);

            var fields = await db.Fields.Where(f => f.TrackerId == dto.TrackerId).ToListAsync();

            var mappings = dto.Mappings
                .Select(m => new FieldMapping(m.SourceKey, m.FieldId, m.SkipWhenNull))
                .ToList();

            var mappingError = MappingValidator.Validate(mappings, provider.Catalog(dto.ResourceType), fields);
            if (mappingError != null)
                return Result.Failure(ResultStatusCodes.BadRequest, mappingError);

            return Result.Success((provider, modeResult.Data, fields));
        }

        private static Result<IntegrationMode> ResolveMode(string? requested, IIntegrationProvider provider)
        {
            var supportsPull = provider.Capabilities.HasFlag(IntegrationCapabilities.Pull);
            var supportsPush = provider.Capabilities.HasFlag(IntegrationCapabilities.Push);

            if (string.IsNullOrWhiteSpace(requested))
                // Pull is the default where a provider offers both: it backfills, which push
                // alone cannot.
                return supportsPull
                    ? Result.Success(IntegrationMode.Pull)
                    : Result.Success(IntegrationMode.Push);

            if (!Enum.TryParse<IntegrationMode>(requested, ignoreCase: true, out var mode))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("mode"));

            var supported = mode == IntegrationMode.Pull ? supportsPull : supportsPush;
            if (!supported)
                return Result.Failure(ResultStatusCodes.BadRequest, $"{provider.DisplayName} does not support {mode} mode.");

            return Result.Success(mode);
        }

        /// <summary>
        /// A user-supplied instance address is a request this server will make, so it is
        /// checked before it is stored: https only, a real host, and no address that would
        /// point the server back at its own network.
        /// </summary>
        private static string? ValidateBaseUrl(string? baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                return null;

            if (!Uri.TryCreate(baseUrl.Trim(), UriKind.Absolute, out var uri))
                return Messages.Invalid("instance address");

            if (uri.Scheme != Uri.UriSchemeHttps)
                return "The instance address must use https.";

            if (uri.IsLoopback)
                return "The instance address cannot point at this server.";

            // A literal private or link-local address is the shape of an attempt to reach
            // something inside this network. A hostname resolving to one is not caught here --
            // that check belongs at request time, with the resolved address in hand.
            if (System.Net.IPAddress.TryParse(uri.Host, out var ip) && IsPrivate(ip))
                return "The instance address cannot be a private network address.";

            return null;
        }

        private static bool IsPrivate(System.Net.IPAddress ip)
        {
            if (System.Net.IPAddress.IsLoopback(ip))
                return true;

            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                var bytes = ip.GetAddressBytes();
                return bytes[0] == 10
                    || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                    || (bytes[0] == 192 && bytes[1] == 168)
                    // Link-local, which is where cloud metadata services live.
                    || (bytes[0] == 169 && bytes[1] == 254);
            }

            return ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal;
        }

        /// <summary>256 bits, URL-safe: used for both the webhook path token and its secret.</summary>
        private static string NewSecret() =>
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace("+", "-").Replace("/", "_").TrimEnd('=');

        private Task<Integration?> LoadOwned(string integrationId)
        {
            var userId = currentUserService.GetCurrentUser().Id;

            return db.Integrations
                .AsTracking()
                .Include(i => i.Targets)
                    .ThenInclude(t => t.Mappings)
                .Include(i => i.Targets)
                    .ThenInclude(t => t.Tracker)
                .FirstOrDefaultAsync(i => i.Id == integrationId && i.UserId == userId);
        }

        // ---- mapping to DTOs ----

        private IntegrationDto ToDto(Integration integration) => new()
        {
            Id = integration.Id,
            Provider = integration.Provider,
            ExternalAccountId = integration.ExternalAccountId,
            BaseUrl = integration.BaseUrl,
            MaskedCredential = credentialProtector.Mask(integration.CredentialCiphertext),
            IsEnabled = integration.IsEnabled,
            CreatedAt = integration.CreatedAt,
            Targets = [.. integration.Targets.Select(t => ToDto(t, integration.Provider))],
        };

        private IntegrationTargetDto ToDto(IntegrationTarget target, string providerKey, string? plaintextSecret = null) => new()
        {
            Id = target.Id,
            TrackerId = target.TrackerId,
            TrackerName = target.Tracker?.Name ?? string.Empty,
            ResourceType = target.ResourceType,
            Mode = target.Mode.ToString(),
            IsEnabled = target.IsEnabled,
            BackfillFrom = target.BackfillFrom,
            LastSyncedAt = target.LastSyncedAt,
            LastSyncStatus = target.LastSyncStatus.ToString(),
            LastSyncError = target.LastSyncError,
            WebhookUrl = target.WebhookToken == null
                ? null
                : $"{configuration["ServerUrl"]?.TrimEnd('/')}/api/integrations/webhooks/{providerKey}/{target.WebhookToken}",
            WebhookSecret = plaintextSecret,
            Mappings = [.. target.Mappings.Select(m => new FieldMappingDto
            {
                SourceKey = m.SourceKey,
                FieldId = m.FieldId,
                SkipWhenNull = m.SkipWhenNull,
            })],
        };
    }
}
