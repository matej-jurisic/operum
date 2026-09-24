using System.Collections.Generic;
using System.Text.RegularExpressions;
using Operum.Model.Extensions;
using Operum.Model.Models;

namespace Operum.Service.Domain.Notifications
{
    // Falls back to the generic body when the user hasn't written a custom template.
    public static class NotificationMessageBuilder
    {
        // Caps entries rendered so a large batch can't blow up the push body.
        public const int MaxEntries = 5;

        private static readonly Regex TokenPattern = new(@"\{([^{}]+)\}", RegexOptions.Compiled);

        public static string Build(string? template, string fallback, IReadOnlyDictionary<string, string> tokens)
        {
            var text = string.IsNullOrWhiteSpace(template) ? fallback : template;
            return Replace(text, tokens);
        }

        // A template that references field names ("{Amount}") is rendered once per entry so each
        // line carries that entry's own values. Otherwise it renders once for the whole batch.
        public static string BuildForEntries(
            string? template,
            string fallback,
            IReadOnlyDictionary<string, string> tokens,
            IReadOnlyList<Entry> orderedEntries,
            int totalCount,
            IReadOnlyCollection<string> fieldNames)
        {
            if (string.IsNullOrWhiteSpace(template) || orderedEntries.Count == 0)
                return Build(template, fallback, tokens);

            var names = new HashSet<string>(fieldNames, StringComparer.OrdinalIgnoreCase);
            var referencesField = TokenPattern.Matches(template)
                .Any(m => names.Contains(m.Groups[1].Value) && !tokens.ContainsKey(m.Groups[1].Value));

            if (!referencesField)
                return Build(template, fallback, tokens);

            var lines = orderedEntries
                .Take(MaxEntries)
                .Select(entry => Replace(template, WithFieldValues(tokens, entry, names)))
                .ToList();

            var remaining = totalCount - lines.Count;
            if (remaining > 0)
                lines.Add(remaining == 1 ? "and 1 more" : $"and {remaining} more");

            return string.Join("\n", lines);
        }

        private static Dictionary<string, string> WithFieldValues(
            IReadOnlyDictionary<string, string> tokens, Entry entry, IEnumerable<string> fieldNames)
        {
            var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var name in fieldNames)
                merged[name] = "-";

            foreach (var fieldValue in entry.FieldValues)
            {
                var value = fieldValue.GetValueAsString();
                merged[fieldValue.Field.Name] = string.IsNullOrEmpty(value) ? "-" : value;
            }

            foreach (var (key, value) in tokens)
                merged[key] = value;

            return merged;
        }

        private static string Replace(string text, IReadOnlyDictionary<string, string> tokens)
        {
            var lookup = new Dictionary<string, string>(tokens, StringComparer.OrdinalIgnoreCase);

            return TokenPattern.Replace(text, m =>
                lookup.TryGetValue(m.Groups[1].Value, out var value) ? value : m.Value);
        }
    }
}
