using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Fields.Requests;
using Operum.Model.DTOs.TrackerConstants.Requests;
using Operum.Tests.Util;
using System.Net;
using System.Net.Http.Json;

namespace Operum.Tests.Tests.Fields
{
    /// <summary>A field's default value is either a static literal or a link to a tracker constant, applied when a new entry is created.</summary>
    public class FieldDefaultValueTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory = factory;

        private Task<HttpClient> OwnerClient() => _factory.NewUserClient("fielddefaults");

        private static async Task<string> CreateConstant(HttpClient client, string trackerId, string name, string type, string value) =>
            await TestApi.IdOf(await client.PostAsJsonAsync($"trackers/{trackerId}/constants",
                new CreateTrackerConstantDto { Name = name, Type = type, Value = value }));

        [Fact]
        public async Task CreateField_WithAStaticDefaultValue_RoundTrips()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Static default");

            var response = await TestApi.PostField(client, trackerId,
                new CreateFieldDto { Name = "Status", Type = DataTypes.String, DefaultValue = "Open" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var field = await TestApi.Data(response);
            Assert.Equal("Open", field.GetProperty("defaultValue").GetString());
        }

        [Fact]
        public async Task CreateField_WithAConstantDefault_RoundTrips()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Constant default");
            var constantId = await CreateConstant(client, trackerId, "DefaultRate", DataTypes.Number, "5");

            var response = await TestApi.PostField(client, trackerId,
                new CreateFieldDto { Name = "Rate", Type = DataTypes.Number, DefaultValueConstantId = constantId });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var field = await TestApi.Data(response);
            Assert.Equal(constantId, field.GetProperty("defaultValueConstantId").GetString());
        }

        [Fact]
        public async Task CreateField_WithBothStaticAndConstantDefault_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Both defaults");
            var constantId = await CreateConstant(client, trackerId, "DefaultRate", DataTypes.Number, "5");

            var response = await TestApi.PostField(client, trackerId, new CreateFieldDto
            {
                Name = "Rate",
                Type = DataTypes.Number,
                DefaultValue = "1",
                DefaultValueConstantId = constantId
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateField_ReferenceTypeWithDefaultValue_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Reference default");
            var otherTrackerId = await TestApi.CreateTracker(client, "Reference target");

            var response = await TestApi.PostField(client, trackerId, new CreateFieldDto
            {
                Name = "Linked",
                Type = DataTypes.Reference,
                ReferencedTrackerId = otherTrackerId,
                DefaultValue = "something"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateField_ConstantDefaultOfIncompatibleType_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Incompatible default");
            var constantId = await CreateConstant(client, trackerId, "Note", DataTypes.String, "hello");

            var response = await TestApi.PostField(client, trackerId,
                new CreateFieldDto { Name = "Rate", Type = DataTypes.Number, DefaultValueConstantId = constantId });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task DeletingTheLinkedConstant_DegradesTheFieldToNoDefault()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Degrade default");
            var constantId = await CreateConstant(client, trackerId, "DefaultRate", DataTypes.Number, "5");
            var fieldId = await TestApi.IdOf(await TestApi.PostField(client, trackerId,
                new CreateFieldDto { Name = "Rate", Type = DataTypes.Number, DefaultValueConstantId = constantId }));

            var deleteResponse = await client.DeleteAsync($"trackers/{trackerId}/constants/{constantId}");
            Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

            var field = await TestApi.Data(await client.GetAsync($"trackers/{trackerId}/fields/{fieldId}"));
            Assert.True(field.GetProperty("defaultValueConstantId").ValueKind is System.Text.Json.JsonValueKind.Null);
        }
    }
}
