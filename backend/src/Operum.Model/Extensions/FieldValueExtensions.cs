using Operum.Model.Constants;
using Operum.Model.Models;
using System.Text.Json;

namespace Operum.Model.Extensions
{
    public static class FieldValueExtensions
    {
        public static object? GetFieldValue(this FieldValue fieldValue)
        {
            if (fieldValue.Field != null)
            {
                return fieldValue.Field.Type.ToLowerInvariant() switch
                {
                    OperumTypes.String => fieldValue.StringValue,
                    OperumTypes.Number => fieldValue.NumberValue,
                    OperumTypes.Date => fieldValue.DateTimeValue,
                    OperumTypes.DateTime => fieldValue.DateTimeValue,
                    OperumTypes.TimeSpan => fieldValue.TimeSpanValue,
                    OperumTypes.Bool => fieldValue.BooleanValue,
                    _ => null,
                };
            }
            return null;
        }

        public static bool SetFieldValue(this FieldValue fieldValue, Field currentField, string? value)
        {
            try
            {
                switch (currentField.Type.ToLowerInvariant())
                {
                    case OperumTypes.String:
                        fieldValue.StringValue = value;
                        break;
                    case OperumTypes.Number:
                        fieldValue.NumberValue = value == null ? null : Convert.ToDouble(value);
                        break;
                    case OperumTypes.Date:
                    case OperumTypes.DateTime:
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            fieldValue.DateTimeValue = DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind).ToUniversalTime();
                        }
                        else if (value == null)
                        {
                            fieldValue.DateTimeValue = null;
                        }
                        else
                        {
                            throw new FormatException($"Invalid format for {currentField.Type} field '{currentField.Name}'.");
                        }
                        break;
                    case OperumTypes.TimeSpan:
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            fieldValue.TimeSpanValue = TimeSpan.Parse(value);
                        }
                        else if (value == null)
                        {
                            fieldValue.TimeSpanValue = null;
                        }
                        else
                        {
                            throw new FormatException($"Invalid format for {currentField.Type} field '{currentField.Name}'.");
                        }
                        break;
                    case OperumTypes.Bool:
                        fieldValue.BooleanValue = value == null ? null : Convert.ToBoolean(value);
                        break;
                    default:
                        return false;
                }
                ClearOtherFieldValues(fieldValue, currentField.Type.ToLowerInvariant());
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void ClearOtherFieldValues(FieldValue fieldValue, string currentType)
        {
            if (currentType != OperumTypes.String) fieldValue.StringValue = null;
            if (currentType != OperumTypes.Number) fieldValue.NumberValue = null;
            if (currentType != OperumTypes.Date && currentType != OperumTypes.DateTime) fieldValue.DateTimeValue = null;
            if (currentType != OperumTypes.TimeSpan) fieldValue.TimeSpanValue = null;
            if (currentType != OperumTypes.Bool) fieldValue.BooleanValue = null;
        }

        private static object? ExtractValueFromJsonElement(JsonElement jsonElement, string targetFieldType)
        {
            try
            {
                switch (jsonElement.ValueKind)
                {
                    case JsonValueKind.String:
                        return jsonElement.GetString();
                    case JsonValueKind.Number:
                        if (jsonElement.TryGetDouble(out double doubleVal))
                        {
                            return doubleVal;
                        }
                        return null;
                    case JsonValueKind.True:
                        return true;
                    case JsonValueKind.False:
                        return false;
                    case JsonValueKind.Null:
                        return null;
                    default:
                        return null;
                }
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }
    }
}
