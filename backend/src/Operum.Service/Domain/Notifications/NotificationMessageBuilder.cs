using System.Collections.Generic;

namespace Operum.Service.Domain.Notifications
{
    // Falls back to the generic body when the user hasn't written a custom template.
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
