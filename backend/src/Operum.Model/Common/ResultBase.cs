using Operum.Model.Enums;

namespace Operum.Model.Common
{
    public abstract class ResultBase
    {
        public IReadOnlyCollection<string> Messages { get; init; } = [];
        public ResultStatusCodes StatusCode { get; init; }

        public bool IsSuccess => StatusCode == ResultStatusCodes.Ok;
        public bool IsFailure => !IsSuccess;
    }
}
