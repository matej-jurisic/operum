using System.Text.Json;
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
        [JsonConverter(typeof(StringBooleanConverter))]
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
        [JsonConverter(typeof(StringLongConverter))]
        public long IssuedAt { get; set; }

        [JsonPropertyName("exp")]
        [JsonConverter(typeof(StringLongConverter))]
        public long ExpirationTime { get; set; }
    }

    /// <summary>
    /// Handles "true"/"false" strings and real JSON booleans.
    /// </summary>
    public class StringBooleanConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString() == "true",
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                _ => throw new JsonException($"Unexpected token {reader.TokenType} when parsing boolean.")
            };
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            writer.WriteBooleanValue(value);
        }
    }

    /// <summary>
    /// Handles numbers returned as strings.
    /// </summary>
    public class StringLongConverter : JsonConverter<long>
    {
        public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => long.Parse(reader.GetString() ?? "0"),
                JsonTokenType.Number => reader.GetInt64(),
                _ => throw new JsonException($"Unexpected token {reader.TokenType} when parsing long.")
            };
        }

        public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }
}
