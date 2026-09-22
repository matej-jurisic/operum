using Operum.Model.Constants;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Fields.Requests;
using Operum.Tests.Util;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Operum.Tests.Tests.Fields
{
    /// <summary>A field can be hidden from the create-entry form based on another field's live value.</summary>
    public class FieldVisibilityTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory = factory;

        private Task<HttpClient> OwnerClient() => _factory.NewUserClient("fieldvisibility");

        [Fact]
        public async Task CreateField_WithAVisibilityCondition_RoundTrips()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Visibility condition");
            var statusId = await TestApi.CreateField(client, trackerId, "Status", DataTypes.String);

            var response = await TestApi.PostField(client, trackerId, new CreateFieldDto
            {
                Name = "Reason",
                Type = DataTypes.String,
                VisibilityFieldId = statusId,
                VisibilityOperator = OperatorTypes.EqualsOperator,
                VisibilityValue = "Refunded"
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var field = await TestApi.Data(response);
            Assert.Equal(statusId, field.GetProperty("visibilityFieldId").GetString());
            Assert.Equal(OperatorTypes.EqualsOperator, field.GetProperty("visibilityOperator").GetString());
            Assert.Equal("Refunded", field.GetProperty("visibilityValue").GetString());
        }

        [Fact]
        public async Task CreateField_VisibilityConditionReferencingItself_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Self-referencing visibility");
            var fieldId = await TestApi.CreateField(client, trackerId, "Reason", DataTypes.String);

            var response = await client.PutAsJsonAsync($"trackers/{trackerId}/fields/{fieldId}", new UpdateFieldDto
            {
                Name = "Reason",
                Type = DataTypes.String,
                VisibilityFieldId = fieldId,
                VisibilityOperator = OperatorTypes.EqualsOperator,
                VisibilityValue = "x"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateField_VisibilityConditionReferencingACalculatedField_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Calculated target visibility");
            var totalId = await TestApi.IdOf(await TestApi.PostField(client, trackerId, new CreateFieldDto
            {
                Name = "Total",
                Type = DataTypes.Number,
                IsCalculated = true,
                Formula = "1 + 1"
            }));

            var response = await TestApi.PostField(client, trackerId, new CreateFieldDto
            {
                Name = "Reason",
                Type = DataTypes.String,
                VisibilityFieldId = totalId,
                VisibilityOperator = OperatorTypes.EqualsOperator,
                VisibilityValue = "2"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateField_CalculatedFieldWithVisibilityCondition_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Calculated visibility");
            var statusId = await TestApi.CreateField(client, trackerId, "Status", DataTypes.String);

            var response = await TestApi.PostField(client, trackerId, new CreateFieldDto
            {
                Name = "Total",
                Type = DataTypes.Number,
                IsCalculated = true,
                Formula = "1 + 1",
                VisibilityFieldId = statusId,
                VisibilityOperator = OperatorTypes.EqualsOperator,
                VisibilityValue = "Refunded"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task DeletingTheTargetField_DegradesTheFieldToAlwaysVisible()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Degrade visibility");
            var statusId = await TestApi.CreateField(client, trackerId, "Status", DataTypes.String);
            var fieldId = await TestApi.IdOf(await TestApi.PostField(client, trackerId, new CreateFieldDto
            {
                Name = "Reason",
                Type = DataTypes.String,
                VisibilityFieldId = statusId,
                VisibilityOperator = OperatorTypes.EqualsOperator,
                VisibilityValue = "Refunded"
            }));

            var deleteResponse = await client.DeleteAsync($"trackers/{trackerId}/fields/{statusId}");
            Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

            var field = await TestApi.Data(await client.GetAsync($"trackers/{trackerId}/fields/{fieldId}"));
            Assert.True(field.GetProperty("visibilityFieldId").ValueKind is JsonValueKind.Null);
        }
    }
}
