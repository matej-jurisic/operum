using System.Text.Json;
using System.Text.Json.Serialization;

namespace Operum.Model.Common
{
    // Tells "the property was absent" apart from "the property was set to null", which a
    // plain nullable cannot. For a hand-edited document that difference is the whole point:
    // a field left out is left alone, a field set to null is cleared.
    // An unset value must be left out of the JSON entirely, not written as null, or reading
    // the document back would turn every inapplicable field into an explicit null. Pair each
    // property with [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)], which
    // is what Equals below answers.
    [JsonConverter(typeof(OptionalConverterFactory))]
    public readonly struct Optional<T> : IEquatable<Optional<T>>
    {
        public bool IsSet { get; }
        public T? Value { get; }

        public Optional(T? value)
        {
            IsSet = true;
            Value = value;
        }

        public static implicit operator Optional<T>(T? value) => new(value);

        public bool Equals(Optional<T> other) =>
            IsSet == other.IsSet && EqualityComparer<T?>.Default.Equals(Value, other.Value);

        public override bool Equals(object? obj) => obj is Optional<T> other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(IsSet, Value);
    }

    public class OptionalConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) =>
            typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Optional<>);

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
            (JsonConverter)Activator.CreateInstance(
                typeof(OptionalConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]))!;
    }

    // Read only runs for a property that is actually there, so reaching it at all is what
    // marks the value as set.
    public class OptionalConverter<T> : JsonConverter<Optional<T>>
    {
        public override Optional<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(JsonSerializer.Deserialize<T>(ref reader, options));

        public override void Write(Utf8JsonWriter writer, Optional<T> value, JsonSerializerOptions options)
        {
            if (!value.IsSet)
            {
                writer.WriteNullValue();
                return;
            }

            JsonSerializer.Serialize(writer, value.Value, options);
        }
    }
}
