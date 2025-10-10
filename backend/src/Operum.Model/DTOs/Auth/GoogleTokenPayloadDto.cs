using Operum.Model.Converters.JSON;
using System.Text.Json.Serialization;

namespace Operum.Model.DTOs.Auth
{
    public class GoogleTokenPayloadDto
    {
        [JsonPropertyName("iss")]
        public string Issuer { get; set; } = string.Empty;

        [JsonPropertyName("azp")]
        public string AuthorizedParty { get; set; } = string.Empty;

        [JsonPropertyName("aud")]
        public string Audience { get; set; } = string.Empty;

        [JsonPropertyName("sub")]
        public string Subject { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("email_verified")]
        [JsonConverter(typeof(StringBooleanFormatter))]
        public bool EmailVerified { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("picture")]
        public string Picture { get; set; } = string.Empty;

        [JsonPropertyName("given_name")]
        public string GivenName { get; set; } = string.Empty;

        [JsonPropertyName("family_name")]
        public string FamilyName { get; set; } = string.Empty;

        [JsonPropertyName("iat")]
        [JsonConverter(typeof(StringLongFormatter))]
        public long IssuedAt { get; set; }

        [JsonPropertyName("exp")]
        [JsonConverter(typeof(StringLongFormatter))]
        public long ExpirationTime { get; set; }
    }
}
