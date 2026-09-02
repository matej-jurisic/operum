using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Operum.Model;
using Operum.Model.Constants.Fields;
using Operum.Model.Constants.Integrations;
using Operum.Model.DTOs.Fields.Requests;
using Operum.Model.DTOs.Integrations;
using Operum.Model.DTOs.Integrations.Requests;
using Operum.Model.DTOs.Trackers.Requests;
using Operum.Model.Models;
using Operum.Service.Integrations.Firefly;
using Operum.Tests.Util;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Operum.Tests.Tests.Integrations
{
    /// <summary>
    /// The push path end to end: a signed delivery arriving at the anonymous webhook route and
    /// landing as entries. This is what proves the provider abstraction holds -- Firefly shares
    /// none of intervals.icu's shape, and everything downstream of the provider is the same code.
    /// </summary>
    public class FireflyWebhookTests(IntegrationsEnabledFactory factory) : IClassFixture<IntegrationsEnabledFactory>
    {
        private static int _userCounter;

        /// <summary>
        /// Firefly mints the webhook secret in its own screen; the user pastes it into Operum.
        /// The tests stand in for that with a fixed value they set through the secret endpoint.
        /// </summary>
        private const string PushSecret = "firefly-test-secret-value";

        private readonly IntegrationsEnabledFactory _factory = factory;

        private static async Task<JsonElement> Data(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(body).RootElement;
            if (!json.TryGetProperty("data", out var data))
                throw new Exception($"Response had no 'data' property. Status: {response.StatusCode}. Body: {body}");
            return data;
        }

        /// <summary>
        /// A tracker, a Firefly connection, and a push target wired onto it, with the secret
        /// already set the way a user would after making the webhook in Firefly.
        /// </summary>
        private async Task<(string TrackerId, string TargetId, string IntegrationId, string Token, string Secret, HttpClient Client, Dictionary<string, string> Fields)>
            Wire(string trackerName)
        {
            var client = await _factory.NewUserClient($"fw{Interlocked.Increment(ref _userCounter)}");

            var trackerId = (await Data(await client.PostAsJsonAsync("trackers", new CreateTrackerDto { Name = trackerName })))
                .GetProperty("id").GetString()!;

            await client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = "Amount", Type = DataTypes.Number });
            await client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = "Description", Type = DataTypes.String });
            await client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = "Category", Type = DataTypes.String });

            var fieldsJson = await Data(await client.GetAsync($"trackers/{trackerId}/fields"));
            var fields = fieldsJson.EnumerateArray().ToDictionary(
                f => f.GetProperty("name").GetString()!,
                f => f.GetProperty("id").GetString()!);

            // Push-only, so connecting calls nothing and stores no credential.
            var integrationId = (await Data(await client.PostAsJsonAsync("integrations",
                new ConnectIntegrationDto { Provider = FireflyProvider.ProviderKey })))
                .GetProperty("id").GetString()!;

            var target = await Data(await client.PostAsJsonAsync($"integrations/{integrationId}/targets",
                new SaveIntegrationTargetDto
                {
                    TrackerId = trackerId,
                    ResourceType = FireflyTransactionCatalog.ResourceType,
                    Mappings =
                    [
                        new FieldMappingDto { SourceKey = FireflyTransactionCatalog.AmountKey, FieldId = fields["Amount"] },
                        new FieldMappingDto { SourceKey = "description", FieldId = fields["Description"] },
                        new FieldMappingDto { SourceKey = "category_name", FieldId = fields["Category"] },
                    ],
                }));

            var webhookUrl = target.GetProperty("webhookUrl").GetString()!;
            var targetId = target.GetProperty("id").GetString()!;

            // Firefly does not accept a chosen secret, so Operum stores none at creation: it
            // comes back afterward once the user has made the webhook in Firefly.
            Assert.True(target.TryGetProperty("webhookSecret", out var createdSecret) is false
                || createdSecret.ValueKind == JsonValueKind.Null);
            Assert.False(target.GetProperty("hasWebhookSecret").GetBoolean());

            await client.PostAsJsonAsync($"integrations/{integrationId}/targets/{targetId}/secret",
                new SetWebhookSecretDto { Secret = PushSecret });

            return (trackerId, targetId, integrationId, webhookUrl.Split('/')[^1], PushSecret, client, fields);
        }

        /// <summary>Posts a delivery the way a real Firefly instance would, unauthenticated.</summary>
        private async Task<HttpResponseMessage> Deliver(string token, string body, string secret, string? overrideSignature = null)
        {
            var client = _factory.CreateClient();
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var request = new HttpRequestMessage(HttpMethod.Post, $"integrations/webhooks/{FireflyProvider.ProviderKey}/{token}")
            {
                Content = new StringContent(body, Encoding.UTF8),
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Headers.TryAddWithoutValidation(FireflySignature.HeaderName,
                overrideSignature ?? $"t={timestamp},v1={FireflySignature.ComputeHex(timestamp, body, secret)}");

            return await client.SendAsync(request);
        }

        private static string Payload(string trigger, string groupId, params string[] splits) => $$"""
            {
              "uuid": "abc",
              "trigger": "{{trigger}}",
              "response": "TRANSACTIONS",
              "content": {
                "id": "{{groupId}}",
                "updated_at": "2026-02-01T10:00:00Z",
                "transactions": [ {{string.Join(",", splits)}} ]
              }
            }
            """;

        private static string Split(string journalId, string amount, string description) => $$"""
            {
              "transaction_journal_id": "{{journalId}}",
              "type": "withdrawal",
              "date": "2026-02-01T09:00:00Z",
              "amount": "{{amount}}",
              "currency_code": "EUR",
              "description": "{{description}}",
              "category_name": "Food"
            }
            """;

        private async Task<List<Entry>> StoredEntries(string trackerId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
            return await db.Entries.Include(e => e.FieldValues)
                .Where(e => e.TrackerId == trackerId).ToListAsync();
        }

        [Fact]
        public async Task Delivery_CreatesOneEntryPerSplit()
        {
            var (trackerId, _, _, token, secret, _, fields) = await Wire("Firefly split");

            var response = await Deliver(token,
                Payload("STORE_TRANSACTION", "g1", Split("j1", "10.00", "Lunch"), Split("j2", "5.00", "Tip")),
                secret);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(2, (await Data(response)).GetProperty("created").GetInt32());

            var entries = await StoredEntries(trackerId);
            Assert.Equal(2, entries.Count);
            Assert.All(entries, e => Assert.Equal("g1", e.ExternalGroupId));
            Assert.Equal(["j1", "j2"], entries.Select(e => e.ExternalId).Order());

            // Signed, because a mixed column only sums to a net if withdrawals are negative.
            Assert.Equal(-10, entries.Single(e => e.ExternalId == "j1")
                .FieldValues.Single(fv => fv.FieldId == fields["Amount"]).NumberValue);
        }

        [Fact]
        public async Task Delivery_IsAcceptedWithoutAnyAuthentication()
        {
            // The caller is the user's own Firefly instance, which has no Operum session.
            var (trackerId, _, _, token, secret, _, _) = await Wire("Firefly anonymous");

            var response = await Deliver(token, Payload("STORE_TRANSACTION", "g1", Split("j1", "1.00", "x")), secret);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Single(await StoredEntries(trackerId));
        }

        [Fact]
        public async Task Delivery_WithABadSignature_IsForbiddenAndWritesNothing()
        {
            var (trackerId, _, _, token, secret, _, _) = await Wire("Firefly bad signature");

            var response = await Deliver(token,
                Payload("STORE_TRANSACTION", "g1", Split("j1", "10.00", "Lunch")),
                secret,
                overrideSignature: "t=1700000000,v1=deadbeef");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Empty(await StoredEntries(trackerId));
        }

        [Fact]
        public async Task Delivery_WithNoSignatureAtAll_IsForbidden()
        {
            var (trackerId, _, _, token, secret, _, _) = await Wire("Firefly no signature");

            var client = _factory.CreateClient();
            var response = await client.PostAsync(
                $"integrations/webhooks/{FireflyProvider.ProviderKey}/{token}",
                new StringContent(Payload("STORE_TRANSACTION", "g1", Split("j1", "1.00", "x")), Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Empty(await StoredEntries(trackerId));
        }

        [Fact]
        public async Task Delivery_ToAnUnknownToken_IsNotFound()
        {
            var response = await Deliver("not-a-real-token", Payload("STORE_TRANSACTION", "g1", Split("j1", "1", "x")), "any");

            // 404 rather than 403, so a caller learns nothing about which half was wrong.
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Delivery_UnderTheWrongProviderName_IsNotFound()
        {
            var (_, _, _, token, secret, _, _) = await Wire("Firefly wrong provider");

            var client = _factory.CreateClient();
            var body = Payload("STORE_TRANSACTION", "g1", Split("j1", "1", "x"));
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var request = new HttpRequestMessage(HttpMethod.Post, $"integrations/webhooks/intervals.icu/{token}")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            request.Headers.TryAddWithoutValidation(FireflySignature.HeaderName,
                $"t={timestamp},v1={FireflySignature.ComputeHex(timestamp, body, secret)}");

            Assert.Equal(HttpStatusCode.NotFound, (await client.SendAsync(request)).StatusCode);
        }

        [Fact]
        public async Task Redelivery_UpdatesRatherThanDuplicating()
        {
            var (trackerId, _, _, token, secret, _, fields) = await Wire("Firefly redelivery");

            await Deliver(token, Payload("STORE_TRANSACTION", "g1", Split("j1", "10.00", "Lunch")), secret);
            var second = await Deliver(token, Payload("UPDATE_TRANSACTION", "g1", Split("j1", "12.00", "Lunch, revised")), secret);

            Assert.Equal(1, (await Data(second)).GetProperty("updated").GetInt32());

            var entry = Assert.Single(await StoredEntries(trackerId));
            Assert.Equal(-12, entry.FieldValues.Single(fv => fv.FieldId == fields["Amount"]).NumberValue);
        }

        [Fact]
        public async Task EditRemovingASplit_DeletesThatSplitsEntry()
        {
            var (trackerId, _, _, token, secret, _, _) = await Wire("Firefly split removal");

            await Deliver(token,
                Payload("STORE_TRANSACTION", "g1", Split("j1", "10.00", "Part one"), Split("j2", "5.00", "Part two")),
                secret);
            Assert.Equal(2, (await StoredEntries(trackerId)).Count);

            // The user edited the transaction down to a single split. Without group
            // reconciliation the second entry -- and its money -- would linger forever.
            var response = await Deliver(token,
                Payload("UPDATE_TRANSACTION", "g1", Split("j1", "15.00", "Merged")),
                secret);

            Assert.Equal(1, (await Data(response)).GetProperty("deleted").GetInt32());

            var entry = Assert.Single(await StoredEntries(trackerId));
            Assert.Equal("j1", entry.ExternalId);
        }

        [Fact]
        public async Task DestroyTrigger_RemovesEveryEntryForTheGroup()
        {
            var (trackerId, _, _, token, secret, _, _) = await Wire("Firefly destroy");

            await Deliver(token,
                Payload("STORE_TRANSACTION", "g1", Split("j1", "10.00", "One"), Split("j2", "5.00", "Two")),
                secret);

            var response = await Deliver(token,
                Payload("DESTROY_TRANSACTION", "g1", Split("j1", "10.00", "One"), Split("j2", "5.00", "Two")),
                secret);

            Assert.Equal(2, (await Data(response)).GetProperty("deleted").GetInt32());
            Assert.Empty(await StoredEntries(trackerId));
        }

        [Fact]
        public async Task Delivery_DoesNotTouchAnotherGroupsEntries()
        {
            var (trackerId, _, _, token, secret, _, _) = await Wire("Firefly group isolation");

            await Deliver(token, Payload("STORE_TRANSACTION", "g1", Split("j1", "10.00", "One")), secret);
            await Deliver(token, Payload("STORE_TRANSACTION", "g2", Split("j2", "20.00", "Two")), secret);

            // Reconciling g1 must leave g2 alone.
            await Deliver(token, Payload("UPDATE_TRANSACTION", "g1", Split("j1", "11.00", "One revised")), secret);

            var entries = await StoredEntries(trackerId);
            Assert.Equal(2, entries.Count);
            Assert.Contains(entries, e => e.ExternalId == "j2");
        }

        [Fact]
        public async Task Delivery_UpdatesTheTargetsSyncStatus()
        {
            var (_, targetId, _, token, secret, _, _) = await Wire("Firefly status");

            await Deliver(token, Payload("STORE_TRANSACTION", "g1", Split("j1", "1.00", "x")), secret);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
            var target = await db.IntegrationTargets.SingleAsync(t => t.Id == targetId);

            // The UI shows push liveness the same way it shows pull.
            Assert.Equal(SyncStatus.Ok, target.LastSyncStatus);
            Assert.NotNull(target.LastSyncedAt);
        }

        [Fact]
        public async Task BadSignature_DoesNotRecordAnErrorOnTheTarget()
        {
            var (_, targetId, _, token, secret, _, _) = await Wire("Firefly no status noise");

            await Deliver(token, Payload("STORE_TRANSACTION", "g1", Split("j1", "1.00", "x")), secret);
            await Deliver(token, Payload("STORE_TRANSACTION", "g1", Split("j1", "1.00", "x")), secret,
                overrideSignature: "t=1700000000,v1=deadbeef");

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
            var target = await db.IntegrationTargets.SingleAsync(t => t.Id == targetId);

            // A forged delivery is not the user's problem; letting it write to the target's
            // status would let anyone with the URL fill it with noise.
            Assert.Equal(SyncStatus.Ok, target.LastSyncStatus);
            Assert.Null(target.LastSyncError);
        }

        [Fact]
        public async Task DisabledTarget_StopsAcceptingDeliveries()
        {
            var (trackerId, targetId, _, token, secret, _, _) = await Wire("Firefly disabled");

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
                var target = await db.IntegrationTargets.AsTracking().SingleAsync(t => t.Id == targetId);
                target.IsEnabled = false;
                await db.SaveChangesAsync();
            }

            var response = await Deliver(token, Payload("STORE_TRANSACTION", "g1", Split("j1", "1.00", "x")), secret);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Empty(await StoredEntries(trackerId));
        }

        [Fact]
        public async Task ProvidedSecret_IsStoredButNeverListedBack()
        {
            var (_, _, _, _, secret, client, _) = await Wire("Firefly secrecy");

            // Once set from Firefly it is stored encrypted and never returned -- the raw value
            // must not appear in any response.
            var listed = await (await client.GetAsync("integrations")).Content.ReadAsStringAsync();
            Assert.DoesNotContain(secret, listed);
        }

        [Fact]
        public async Task DeliveryBeforeTheSecretIsSet_IsRejectedAndFlagged()
        {
            var client = await _factory.NewUserClient($"fw{Interlocked.Increment(ref _userCounter)}");

            var trackerId = (await Data(await client.PostAsJsonAsync("trackers", new CreateTrackerDto { Name = "Firefly no secret" })))
                .GetProperty("id").GetString()!;
            await client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = "Amount", Type = DataTypes.Number });

            var fieldsJson = await Data(await client.GetAsync($"trackers/{trackerId}/fields"));
            var amountId = fieldsJson.EnumerateArray().First().GetProperty("id").GetString()!;

            var integrationId = (await Data(await client.PostAsJsonAsync("integrations",
                new ConnectIntegrationDto { Provider = FireflyProvider.ProviderKey })))
                .GetProperty("id").GetString()!;

            var created = await Data(await client.PostAsJsonAsync($"integrations/{integrationId}/targets",
                new SaveIntegrationTargetDto
                {
                    TrackerId = trackerId,
                    ResourceType = FireflyTransactionCatalog.ResourceType,
                    Mappings = [new FieldMappingDto { SourceKey = FireflyTransactionCatalog.AmountKey, FieldId = amountId }],
                }));

            var targetId = created.GetProperty("id").GetString()!;
            var token = created.GetProperty("webhookUrl").GetString()!.Split('/')[^1];

            var response = await Deliver(token, Payload("STORE_TRANSACTION", "g1", Split("j1", "1.00", "x")), "anything");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Empty(await StoredEntries(trackerId));

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
            var target = await db.IntegrationTargets.SingleAsync(t => t.Id == targetId);
            Assert.Equal(SyncStatus.Error, target.LastSyncStatus);
        }

        [Fact]
        public async Task SetSecret_ReplacesTheOldOneAndInvalidatesIt()
        {
            var (_, targetId, integrationId, token, oldSecret, client, _) = await Wire("Firefly reset");

            var body = Payload("STORE_TRANSACTION", "g1", Split("j1", "1.00", "x"));
            Assert.Equal(HttpStatusCode.OK, (await Deliver(token, body, oldSecret)).StatusCode);

            // The user reset the secret in Firefly and pasted the new value in.
            const string newSecret = "firefly-rotated-secret-value";
            await client.PostAsJsonAsync($"integrations/{integrationId}/targets/{targetId}/secret",
                new SetWebhookSecretDto { Secret = newSecret });

            Assert.Equal(HttpStatusCode.Forbidden, (await Deliver(token, body, oldSecret)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await Deliver(token, body, newSecret)).StatusCode);
        }

        [Fact]
        public async Task SetSecret_WithNoValue_IsRefusedForAProviderThatSuppliesItsOwn()
        {
            var (_, targetId, integrationId, _, _, client, _) = await Wire("Firefly empty secret");

            var response = await client.PostAsJsonAsync(
                $"integrations/{integrationId}/targets/{targetId}/secret",
                new SetWebhookSecretDto { Secret = null });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
