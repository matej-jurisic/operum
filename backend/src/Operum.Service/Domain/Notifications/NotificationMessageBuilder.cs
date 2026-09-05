using System.Collections.Generic;

namespace Operum.Service.Domain.Notifications
{
    /// <summary>
    /// Resolves a notification's custom push body, if any, against a small set of tokens.
    /// Keeps the default, generic body when the user hasn't written their own.
    /// </summary>
    public static class NotificationMessageBuilder
    {
        public static string Build(string? template, string fallback, IReadOnlyDictionary<string, string> tokens)
        {
            var text = string.IsNullOrWhiteSpace(template) ? fallback : template;

            foreach (var (key, value) in tokens)
                text = text.Replace("{" + key + "}", value);

            return text;
        }
    }
}
