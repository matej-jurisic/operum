using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NCalc2;
using Operum.Model;
using Operum.Model.Constants.Fields;
using Operum.Model.Models;
using Operum.Service.Domain.Constants;
using Operum.Service.Interfaces;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Operum.Service.Services.Fields
{
    public class FormulaEvaluationService(OperumContext db, ILogger<FormulaEvaluationService> logger) : IFormulaEvaluationService
    {
        private static readonly Regex TokenPattern = new(@"\{([^}]+)\}", RegexOptions.Compiled);

        public static IEnumerable<string> ExtractTokenNames(string formula) =>
            TokenPattern.Matches(formula).Select(m => m.Groups[1].Value);

        public async Task EvaluateAndPersistCalculatedFields(
            string trackerId,
            string entryId,
            List<FieldValue> currentFieldValues,
            List<Field> allFields)
        {
            var constants = await db.TrackerConstants
                .Include(c => c.Values)
                    .ThenInclude(v => v.Filters)
                .Where(c => c.TrackerId == trackerId)
                .ToListAsync();

            var manualFieldsByName = allFields
                .Where(f => !f.IsCalculated)
                .ToDictionary(f => f.Name, f => f, StringComparer.OrdinalIgnoreCase);

            var fieldValuesByFieldId = currentFieldValues
                .ToDictionary(fv => fv.FieldId, fv => fv);

            var constantsByName = constants
                .ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);

            var calculatedFields = allFields
                .Where(f => f.IsCalculated && !string.IsNullOrWhiteSpace(f.Formula))
                .ToList();

            var fieldsByIdForMatcher = allFields
                .Where(f => !f.IsCalculated)
                .ToDictionary(f => f.Id, f => f);

            var newFieldValues = new List<FieldValue>();

            // Build a lookup from the already-tracked currentFieldValues so we never
            // issue a second query that could return a duplicate tracked instance.
            var existingByFieldId = currentFieldValues
                .ToDictionary(fv => fv.FieldId, fv => fv);

            foreach (var field in calculatedFields)
            {
                var tokens = ExtractTokenNames(field.Formula!).ToList();
                var substitutedFormula = field.Formula!;
                bool anyMissing = false;

                foreach (var token in tokens)
                {
                    var resolved = TryResolveTokenValue(token, manualFieldsByName, fieldValuesByFieldId, constantsByName, fieldsByIdForMatcher);
                    if (resolved == null)
                    {
                        anyMissing = true;
                        break;
                    }
                    substitutedFormula = substitutedFormula.Replace(
                        "{" + token + "}",
                        resolved.Value.ToString(CultureInfo.InvariantCulture));
                }

                existingByFieldId.TryGetValue(field.Id, out var existing);

                if (anyMissing)
                {
                    if (existing != null)
                        db.FieldValues.Remove(existing);
                    continue;
                }

                double result;
                try
                {
                    var evalResult = new Expression(substitutedFormula).Evaluate();
                    result = evalResult switch
                    {
                        bool b => b ? 1.0 : 0.0,
                        _ => Convert.ToDouble(evalResult, CultureInfo.InvariantCulture)
                    };
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Formula evaluation failed for field {FieldId}: {Formula}", field.Id, substitutedFormula);
                    if (existing != null)
                        db.FieldValues.Remove(existing);
                    continue;
                }

                result = Math.Round(result, 10);

                if (double.IsNaN(result) || double.IsInfinity(result))
                {
                    logger.LogDebug("Formula produced non-finite result for field {FieldId}: {Formula}", field.Id, substitutedFormula);
                    if (existing != null)
                        db.FieldValues.Remove(existing);
                    continue;
                }

                if (existing != null)
                {
                    ApplyResult(existing, field.Type, result);
                }
                else
                {
                    var newFv = new FieldValue { EntryId = entryId, FieldId = field.Id };
                    ApplyResult(newFv, field.Type, result);
                    newFieldValues.Add(newFv);
                }
            }

            if (newFieldValues.Count > 0)
                await db.FieldValues.AddRangeAsync(newFieldValues);

            await db.SaveChangesAsync();
        }

        private static void ApplyResult(FieldValue fv, string fieldType, double result)
        {
            fv.NumberValue = null;
            fv.TimeSpanValue = null;
            fv.BooleanValue = null;
            fv.StringValue = null;
            fv.DateTimeValue = null;

            switch (fieldType)
            {
                case DataTypes.Number:
                    fv.NumberValue = result;
                    break;
                case DataTypes.TimeSpan:
                    fv.TimeSpanValue = TimeSpan.FromSeconds(result);
                    break;
                case DataTypes.Bool:
                    fv.BooleanValue = result != 0.0;
                    break;
            }
        }

        // Parse "FieldName.property" into (name, property). Property is null if no dot.
        private static (string Name, string? Property) ParseToken(string tokenName)
        {
            var dot = tokenName.IndexOf('.');
            if (dot < 0) return (tokenName, null);
            return (tokenName[..dot], tokenName[(dot + 1)..]);
        }

        private static double? TryResolveTokenValue(
            string tokenName,
            Dictionary<string, Field> manualFieldsByName,
            Dictionary<string, FieldValue> fieldValuesByFieldId,
            Dictionary<string, TrackerConstant> constantsByName,
            Dictionary<string, Field> fieldsByIdForMatcher)
        {
            var (name, property) = ParseToken(tokenName);

            // Fields take precedence over constants
            if (manualFieldsByName.TryGetValue(name, out var field))
            {
                if (!fieldValuesByFieldId.TryGetValue(field.Id, out var fv))
                    return null;

                if (field.Type == DataTypes.TimeSpan)
                {
                    if (fv.TimeSpanValue is not { } ts) return null;
                    return property?.ToLowerInvariant() switch
                    {
                        "hours" => ts.TotalHours,
                        "minutes" => ts.TotalMinutes,
                        "seconds" or null => ts.TotalSeconds,
                        _ => null
                    };
                }

                // Non-timespan fields don't support property access
                if (property != null) return null;

                return field.Type switch
                {
                    DataTypes.Number => fv.NumberValue,
                    DataTypes.Bool => fv.BooleanValue.HasValue ? (fv.BooleanValue.Value ? 1.0 : 0.0) : null,
                    _ => null
                };
            }

            // Constants don't support property access
            if (property != null) return null;

            if (constantsByName.TryGetValue(name, out var constant))
            {
                var rawValue = constant.Value;

                if (constant.Values.Count > 0)
                {
                    var match = constant.Values
                        .OrderBy(v => v.Priority)
                        .FirstOrDefault(v => EntryFilterMatcher.Matches(v.Filters, fieldValuesByFieldId, fieldsByIdForMatcher));
                    if (match != null)
                        rawValue = match.Value;
                }

                return constant.Type switch
                {
                    DataTypes.Number => double.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var n) ? n : null,
                    DataTypes.Bool => bool.TryParse(rawValue, out var b) ? (b ? 1.0 : 0.0) : null,
                    DataTypes.TimeSpan => TimeSpan.TryParse(rawValue, out var ts) ? ts.TotalSeconds : null,
                    _ => null
                };
            }

            return null;
        }
    }
}
