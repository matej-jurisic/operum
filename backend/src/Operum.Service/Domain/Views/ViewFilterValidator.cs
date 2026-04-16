using Operum.Model.Constants;
using Operum.Model.Constants.Fields;
using Operum.Model.Converters;


namespace Operum.Service.Domain.Views
{
    public static class ViewFilterValidator
    {
        public static bool IsValidFieldValue(string? value, string fieldType)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(value))
                    return true;

                return fieldType.ToLowerInvariant() switch
                {
                    DataTypes.String => true,
                    DataTypes.Number => double.TryParse(value, out _),
                    DataTypes.Date or DataTypes.DateTime =>
                        DynamicDateTokens.All.Contains(value) || DataFormatters.StringToDateTime(value) != null,
                    DataTypes.TimeSpan => TimeSpan.TryParse(value, out _),
                    DataTypes.Bool => bool.TryParse(value, out _),
                    _ => false,
                };
            }
            catch
            {
                return false;
            }
        }

        public static bool IsValidOperatorForFieldType(string operatorType, string fieldType)
        {
            if (!OperatorTypes.IsValid(operatorType))
                return false;

            var lowerFieldType = fieldType.ToLowerInvariant();

            return lowerFieldType switch
            {
                DataTypes.String => operatorType is OperatorTypes.EqualsOperator or OperatorTypes.NotEquals
                    or OperatorTypes.Contains or OperatorTypes.StartsWith or OperatorTypes.EndsWith,

                DataTypes.Number => operatorType is OperatorTypes.EqualsOperator or OperatorTypes.NotEquals
                    or OperatorTypes.GreaterThan or OperatorTypes.GreaterThanOrEqual
                    or OperatorTypes.LessThan or OperatorTypes.LessThanOrEqual,

                DataTypes.Date or DataTypes.DateTime => operatorType is OperatorTypes.EqualsOperator
                    or OperatorTypes.NotEquals or OperatorTypes.GreaterThan or OperatorTypes.GreaterThanOrEqual
                    or OperatorTypes.LessThan or OperatorTypes.LessThanOrEqual,

                DataTypes.TimeSpan => operatorType is OperatorTypes.EqualsOperator or OperatorTypes.NotEquals
                    or OperatorTypes.GreaterThan or OperatorTypes.GreaterThanOrEqual
                    or OperatorTypes.LessThan or OperatorTypes.LessThanOrEqual,

                DataTypes.Bool => operatorType is OperatorTypes.EqualsOperator or OperatorTypes.NotEquals,

                _ => false
            };
        }
    }
}