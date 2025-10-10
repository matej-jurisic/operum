using Operum.Model.Common;
using Operum.Model.Models;

namespace Operum.Service.Domain.Analytics
{
    public interface ISingleValueCalculator
    {
        Result<(string Value, string? EntryId)> Calculate(List<FieldValue> fieldValues);
    }
}
