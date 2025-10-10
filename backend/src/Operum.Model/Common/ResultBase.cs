using Operum.Model.Enums;

namespace Operum.Model.Common
{
    public abstract class ResultBase
    {
        public IReadOnlyCollection<string> Messages { get; init; } = [];
        public ResultStatus StatusCode { get; init; }

        public bool IsSuccess => StatusCode == ResultStatus.Ok;
        public bool IsFailure => !IsSuccess;
    }
}
