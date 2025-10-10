using System.Globalization;
using System.Text;

namespace Operum.Model.Formatters
{
    public static class StringFormatters
    {
        public static string ToAscii(string input)
        {
            return string.Concat(
                        input
                            .Normalize(NormalizationForm.FormD)
                            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                            .Select(c => char.ToLowerInvariant(c))
                            .Where(c => (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')));
        }

        public static string Capitalize(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return char.ToUpper(input[0]) + input[1..];
        }
    }
}
