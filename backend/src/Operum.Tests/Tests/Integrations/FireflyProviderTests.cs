using Microsoft.Extensions.Logging.Abstractions;
using Operum.Model.Constants.Integrations;
using Operum.Model.Enums;
using Operum.Model.Integrations;
using Operum.Service.Integrations.Firefly;

namespace Operum.Tests.Tests.Integrations
{
    public class FireflyProviderTests
    {
        private const string Secret = "webhook-secret";

        private static FireflyProvider Provider() => new(NullLogger<FireflyProvider>.Instance);

        /// <summary>Signs a body the way a real Firefly instance would.</summary>
        private static Dictionary<string, string> SignedHeaders(string body, DateTime? at = null, string? secret = null)
        {
            var timestamp = new DateTimeOffset(at ?? DateTime.UtcNow).ToUnixTimeSeconds();
            var hex = FireflySignature.ComputeHex(timestamp, body, secret ?? Secret);
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [FireflySignature.HeaderName] = $"t={timestamp},v1={hex}",
            };
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

        private static string Split(
            string journalId, string type = "withdrawal", string amount = "12.50", string description = "Coffee") => $$"""
            {
              "transaction_journal_id": "{{journalId}}",
              "type": "{{type}}",
              "date": "2026-02-01T09:00:00Z",
              "amount": "{{amount}}",
              "currency_code": "EUR",
              "description": "{{description}}",
              "category_name": "Food",
              "source_name": "Checking",
              "destination_name": "Cafe",
              "tags": ["daily", "out"]
            }
            """;

        private static Model.Common.Result<IReadOnlyList<SourceRecord>> Parse(
            FireflyProvider provider, string body, Dictionary<string, string>? headers = null) =>
            provider.VerifyAndParse(
                FireflyTransactionCatalog.ResourceType, Secret, body, headers ?? SignedHeaders(body));

        // ---- signature ----

        [Fact]
        public void Signature_RoundTrips()
        {
            var body = """{"hello":"world"}""";
            var headers = SignedHeaders(body);

            Assert.Equal(FireflySignature.Outcome.Valid,
                FireflySignature.Verify(headers[FireflySignature.HeaderName], body, Secret, DateTime.UtcNow));
        }

        [Fact]
        public void Signature_TamperedBody_DoesNotVerify()
        {
            var body = """{"hello":"world"}""";
            var headers = SignedHeaders(body);

            Assert.Equal(FireflySignature.Outcome.Mismatch,
                FireflySignature.Verify(headers[FireflySignature.HeaderName], body + " ", Secret, DateTime.UtcNow));
        }

        [Fact]
        public void Signature_WrongSecret_DoesNotVerify()
        {
            var body = """{"hello":"world"}""";
            var headers = SignedHeaders(body, secret: "someone-elses-secret");

            Assert.Equal(FireflySignature.Outcome.Mismatch,
                FireflySignature.Verify(headers[FireflySignature.HeaderName], body, Secret, DateTime.UtcNow));
        }

        [Fact]
        public void Signature_OutsideTheReplayWindow_IsExpired()
        {
            var body = """{"hello":"world"}""";
            var old = DateTime.UtcNow - FireflySignature.MaxAge - TimeSpan.FromMinutes(1);

            Assert.Equal(FireflySignature.Outcome.Expired,
                FireflySignature.Verify(SignedHeaders(body, old)[FireflySignature.HeaderName], body, Secret, DateTime.UtcNow));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("garbage")]
        [InlineData("t=123")]                      // no signature part
        [InlineData("v1=abc")]                     // no timestamp
        [InlineData("t=notanumber,v1=abc")]
        [InlineData("t=1700000000,v1=nothex!!")]
        public void Signature_MalformedHeaders_AreRefused(string? header)
        {
            var outcome = FireflySignature.Verify(header, "{}", Secret, DateTime.UtcNow);
            Assert.NotEqual(FireflySignature.Outcome.Valid, outcome);
        }

        [Fact]
        public void Signature_UnknownSchemeParts_AreIgnored()
        {
            // A future v2 alongside v1 must not stop v1 from being read.
            var body = """{"hello":"world"}""";
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var hex = FireflySignature.ComputeHex(timestamp, body, Secret);

            Assert.Equal(FireflySignature.Outcome.Valid,
                FireflySignature.Verify($"t={timestamp},v1={hex},v2=whatever", body, Secret, DateTime.UtcNow));
        }

        // ---- parsing ----

        [Fact]
        public void Parse_BadSignature_YieldsForbiddenAndNoRecords()
        {
            var body = Payload("STORE_TRANSACTION", "g1", Split("j1"));
            var headers = new Dictionary<string, string> { [FireflySignature.HeaderName] = "t=1,v1=deadbeef" };

            var result = Parse(Provider(), body, headers);

            Assert.True(result.IsFailure);
            Assert.Equal(ResultStatusCodes.Forbidden, result.StatusCode);
        }

        [Fact]
        public void Parse_SplitTransaction_ProducesOneRecordPerSplit()
        {
            var body = Payload("STORE_TRANSACTION", "g1",
                Split("j1", amount: "10.00", description: "Part one"),
                Split("j2", amount: "5.00", description: "Part two"));

            var records = Parse(Provider(), body).Data;

            // Keyed on the split, not the group -- keying on the group would collapse these
            // into one entry and lose money.
            Assert.Equal(2, records.Count);
            Assert.Equal(["j1", "j2"], records.Select(r => r.ExternalId));
            Assert.All(records, r => Assert.Equal("g1", r.GroupId));
        }

        [Fact]
        public void Parse_WithdrawalAmount_IsNegative()
        {
            var body = Payload("STORE_TRANSACTION", "g1", Split("j1", type: "withdrawal", amount: "12.50"));
            var record = Assert.Single(Parse(Provider(), body).Data);

            // Firefly reports positive with a type; a mixed column only sums correctly if the
            // sign is real.
            Assert.Equal("-12.5", record.ValuesBySourceKey[FireflyTransactionCatalog.AmountKey]);
        }

        [Fact]
        public void Parse_DepositAmount_IsPositive()
        {
            var body = Payload("STORE_TRANSACTION", "g1", Split("j1", type: "deposit", amount: "200"));
            var record = Assert.Single(Parse(Provider(), body).Data);

            Assert.Equal("200", record.ValuesBySourceKey[FireflyTransactionCatalog.AmountKey]);
        }

        [Fact]
        public void Parse_TransferAmount_IsLeftPositive()
        {
            // A transfer moves money between the user's own accounts: neither income nor
            // expense, so signing it either way would distort a total.
            var body = Payload("STORE_TRANSACTION", "g1", Split("j1", type: "transfer", amount: "75"));
            var record = Assert.Single(Parse(Provider(), body).Data);

            Assert.Equal("75", record.ValuesBySourceKey[FireflyTransactionCatalog.AmountKey]);
        }

        [Fact]
        public void Parse_CarriesTheIdsAndTheOrdinaryFields()
        {
            var body = Payload("STORE_TRANSACTION", "g99", Split("j42"));
            var record = Assert.Single(Parse(Provider(), body).Data);

            Assert.Equal("j42", record.ValuesBySourceKey[FireflyTransactionCatalog.JournalIdKey]);
            Assert.Equal("g99", record.ValuesBySourceKey[FireflyTransactionCatalog.GroupIdKey]);
            Assert.Equal("Coffee", record.ValuesBySourceKey["description"]);
            Assert.Equal("Food", record.ValuesBySourceKey["category_name"]);
            Assert.Equal("EUR", record.ValuesBySourceKey["currency_code"]);
        }

        [Fact]
        public void Parse_JoinsTags()
        {
            var body = Payload("STORE_TRANSACTION", "g1", Split("j1"));
            var record = Assert.Single(Parse(Provider(), body).Data);

            // A list has no field type of its own.
            Assert.Equal("daily, out", record.ValuesBySourceKey["tags"]);
        }

        [Fact]
        public void Parse_Destroy_ProducesDeletes()
        {
            var body = Payload("DESTROY_TRANSACTION", "g1", Split("j1"), Split("j2"));
            var records = Parse(Provider(), body).Data;

            Assert.Equal(2, records.Count);
            Assert.All(records, r => Assert.Equal(SourceOperation.Delete, r.Operation));
        }

        [Fact]
        public void Parse_SplitWithoutAJournalId_IsSkipped()
        {
            var body = Payload("STORE_TRANSACTION", "g1",
                """{ "type": "withdrawal", "amount": "1.00" }""",
                Split("j2"));

            // Without the split's own id there is no idempotency key.
            var record = Assert.Single(Parse(Provider(), body).Data);
            Assert.Equal("j2", record.ExternalId);
        }

        [Fact]
        public void Parse_PayloadWithNoContent_YieldsNothingRatherThanFailing()
        {
            var body = """{ "uuid": "abc", "trigger": "STORE_TRANSACTION" }""";
            var result = Parse(Provider(), body);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Data);
        }

        [Fact]
        public void Parse_NotJson_IsABadRequest()
        {
            var body = "this is not json";
            var result = Parse(Provider(), body);

            Assert.True(result.IsFailure);
            Assert.Equal(ResultStatusCodes.BadRequest, result.StatusCode);
        }

        [Fact]
        public void Parse_UnparseableAmount_ReadsAsNull()
        {
            var body = Payload("STORE_TRANSACTION", "g1",
                """{ "transaction_journal_id": "j1", "type": "withdrawal", "amount": "not-a-number", "description": "kept" }""");

            var record = Assert.Single(Parse(Provider(), body).Data);

            Assert.Null(record.ValuesBySourceKey[FireflyTransactionCatalog.AmountKey]);
            Assert.Equal("kept", record.ValuesBySourceKey["description"]);
        }

        [Fact]
        public void Capabilities_ArePushOnlyAndNeedNoBaseUrl()
        {
            var provider = Provider();

            Assert.Equal(IntegrationCapabilities.Push, provider.Capabilities);
            // The whole reason it is push: the user's instance never has to be reachable.
            Assert.False(provider.RequiresBaseUrl);
        }
    }
}
