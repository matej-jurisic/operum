using Operum.Model.Constants;
using Operum.Model.Models;
using System.Globalization;

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
                        fieldValue.NumberValue = value != null ? Convert.ToDouble(value) : null;
                        break;
                    case DataTypes.Date:
                    case DataTypes.DateTime:
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            fieldValue.DateTimeValue = DataHelpers.GetDateTimeFromString(value);
                        }
                        else
                        {
                            fieldValue.DateTimeValue = null;
                        }
                        break;
                    case DataTypes.TimeSpan:
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            fieldValue.TimeSpanValue = TimeSpan.Parse(value);
                        }
                        else
                        {
                            fieldValue.TimeSpanValue = null;
                        }
                        break;
                    case DataTypes.Bool:
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            fieldValue.BooleanValue = Convert.ToBoolean(value);
                        }
                        else
                        {
                            fieldValue.BooleanValue = null;
                        }
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

        public static string? GetValueAsString(this FieldValue fieldValue)
        {
            if (fieldValue.Field == null) return null;

            return fieldValue.Field.Type.ToLowerInvariant() switch
            {
                DataTypes.String => fieldValue.StringValue,
                DataTypes.Number => fieldValue.NumberValue?.ToString(CultureInfo.InvariantCulture),
                DataTypes.Date => fieldValue.DateTimeValue?.ToString("dd/MM/yyyy"),
                DataTypes.DateTime => fieldValue.DateTimeValue?.ToString("dd/MM/yyyy HH:mm:ss"),
                DataTypes.TimeSpan => fieldValue.TimeSpanValue?.ToString(),
                DataTypes.Bool => fieldValue.BooleanValue?.ToString(),
                _ => null
            };
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
