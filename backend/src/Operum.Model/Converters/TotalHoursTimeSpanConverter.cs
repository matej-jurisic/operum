using System.Text.Json;
using System.Text.Json.Serialization;

namespace Operum.Model.Converters
{
    public class TotalHoursTimeSpanConverter : JsonConverter<TimeSpan?>
    {
        public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            var str = reader.GetString();
            if (TimeSpan.TryParse(str, out var ts))
                return ts;

            return null;
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                var totalHours = (int)value.Value.TotalHours;
                writer.WriteStringValue($"{totalHours:D2}:{value.Value.Minutes:D2}:{value.Value.Seconds:D2}");
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}
