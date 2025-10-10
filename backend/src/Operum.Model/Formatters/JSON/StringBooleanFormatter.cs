using System.Text.Json;
using System.Text.Json.Serialization;

namespace Operum.Model.Converters.JSON
{
    public class StringBooleanFormatter : JsonConverter<bool>
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
}
