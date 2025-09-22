using System.Globalization;
using System.Text;

namespace Operum.Service.Helpers
{
    public static class StringHelpers
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
    }
}
