using Operum.Model.Constants;
using Operum.Model.Models;

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
                    DataTypes.String => fieldValue.StringValue,
                    DataTypes.Number => fieldValue.NumberValue,
                    DataTypes.Date => fieldValue.DateTimeValue,
                    DataTypes.DateTime => fieldValue.DateTimeValue,
                    DataTypes.TimeSpan => fieldValue.TimeSpanValue,
                    DataTypes.Bool => fieldValue.BooleanValue,
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
                    case DataTypes.String:
                        fieldValue.StringValue = !string.IsNullOrWhiteSpace(value) ? value : null;
                        break;
                    case DataTypes.Number:
                        fieldValue.NumberValue = value == null ? null : Convert.ToDouble(value);
                        break;
                    case DataTypes.Date:
                    case DataTypes.DateTime:
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
                    case DataTypes.TimeSpan:
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
                    case DataTypes.Bool:
                        fieldValue.BooleanValue = value != null && Convert.ToBoolean(value);
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
            if (currentType != DataTypes.String) fieldValue.StringValue = null;
            if (currentType != DataTypes.Number) fieldValue.NumberValue = null;
            if (currentType != DataTypes.Date && currentType != DataTypes.DateTime) fieldValue.DateTimeValue = null;
            if (currentType != DataTypes.TimeSpan) fieldValue.TimeSpanValue = null;
            if (currentType != DataTypes.Bool) fieldValue.BooleanValue = null;
        }
    }
}
