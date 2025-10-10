using System.Text.Json;
using System.Text.Json.Serialization;

namespace Operum.Model.Converters.JSON
{
    public class StringLongFormatter : JsonConverter<long>
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
