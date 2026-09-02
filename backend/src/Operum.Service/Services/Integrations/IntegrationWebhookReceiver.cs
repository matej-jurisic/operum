using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.DTOs.Integrations;
using Operum.Model.Enums;
using Operum.Model.Extensions;
using Operum.Model.Integrations;
using Operum.Model.Models;
using Operum.Service.Domain.Integrations;
using Operum.Service.Integrations;
using Operum.Service.Interfaces;

namespace Operum.Service.Services.Integrations
{
    public class IntegrationWebhookReceiver(
        OperumContext db,
        IIntegrationProviderRegistry registry,
        ICredentialProtector credentialProtector,
        IEntryWriter entryWriter,
        ILogger<IntegrationWebhookReceiver> logger) : IIntegrationWebhookReceiver
    {
        public async Task<Result<SyncResultDto>> Receive(
            string providerKey,
            string token,
            string rawBody,
            IReadOnlyDictionary<string, string> headers,
            CancellationToken ct = default)
        {
            var target = await db.IntegrationTargets
                .AsTracking()
                .Include(t => t.Integration)
                .Include(t => t.Mappings)
                .Include(t => t.Tracker)
                    .ThenInclude(t => t.Owner)
                .FirstOrDefaultAsync(t => t.WebhookToken == token, ct);

            // Everything that could be wrong about the address answers the same way. A caller
            // with a bad token learns nothing about which half was wrong, or whether the
            // provider name even exists.
            if (target == null
                || target.Integration.Provider != providerKey
                || target.Mode != IntegrationMode.Push)
            {
                logger.LogWarning("Rejected a webhook delivery for an unknown {Provider} token", providerKey);
                return Result.Failure(ResultStatusCodes.NotFound, "Unknown webhook.");
            }

            if (!target.IsEnabled || !target.Integration.IsEnabled)
                return Result.Failure(ResultStatusCodes.NotFound, "Unknown webhook.");

            var provider = registry.GetPush(providerKey);
            if (provider == null)
                return await Fail(target, $"No integration provider is installed for '{providerKey}'.", ResultStatusCodes.NotFound, ct);

            if (string.IsNullOrEmpty(target.WebhookSecretCiphertext))
                return await Fail(target, "This webhook has no secret yet. Add the one from your provider in Operum.", ResultStatusCodes.Forbidden, ct);

            var secret = credentialProtector.Unprotect(target.WebhookSecretCiphertext);
            if (secret == null)
                return await Fail(target, "The webhook secret could not be read. Set it again and update the provider.", ResultStatusCodes.Forbidden, ct);

            var parsed = provider.VerifyAndParse(target.ResourceType, secret, rawBody, headers);
            if (parsed.IsFailure)
            {
                // A failed signature is not the target's fault and must not be recorded as a
                // sync error -- an attacker could otherwise fill a user's status with noise.
                return Result.Failure(parsed.StatusCode, parsed.Messages);
            }

            if (parsed.Data.Count == 0)
                return Result.Success(new SyncResultDto(), "Nothing to apply.");

            var mappings = target.Mappings
                .Select(m => new FieldMapping(m.SourceKey, m.FieldId, m.SkipWhenNull))
                .ToList();

            var fields = await db.Fields.Where(f => f.TrackerId == target.TrackerId).ToListAsync(ct);
            var timeZone = TimeZoneResolver.FromId(target.Tracker.Owner?.TimeZone);

            var records = SourceRecordProjector.Project(parsed.Data, mappings);

            EntryWriteResult written;
            try
            {
                written = await entryWriter.ApplyAsync(
                    target.TrackerId, providerKey, records, fields, timeZone, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to apply a webhook delivery for target {TargetId}", target.Id);
                return await Fail(target, "The delivery could not be applied. See the server log for details.", ResultStatusCodes.Error, ct);
            }

            target.LastSyncedAt = DateTime.UtcNow;
            target.LastSyncStatus = SyncStatus.Ok;
            target.LastSyncError = null;
            await db.SaveChangesAsync(ct);

            return Result.Success(new SyncResultDto
            {
                Created = written.Created,
                Updated = written.Updated,
                Deleted = written.Deleted,
                Skipped = written.Skipped,
                ErrorCount = written.ErrorCount,
                Errors = written.Errors,
            });
        }

        private async Task<Result<SyncResultDto>> Fail(
            IntegrationTarget target, string message, ResultStatusCodes statusCode, CancellationToken ct)
        {
            target.LastSyncedAt = DateTime.UtcNow;
            target.LastSyncStatus = SyncStatus.Error;
            target.LastSyncError = message;
            await db.SaveChangesAsync(ct);

            return Result.Failure(statusCode, message);
        }
    }
}
