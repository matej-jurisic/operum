using Operum.Model.Enums;

namespace Operum.Model.Common
{
    public class Result : ResultBase
    {
        public static Result Success(string? message = null) => new()
        {
            StatusCode = ResultStatus.Ok,
            Messages = message != null ? [message] : []
        };

        public static Result<T> Success<T>(T data, string? message = null) => new()
        {
            Data = data,
            StatusCode = ResultStatus.Ok,
            Messages = message != null ? [message] : []
        };

        public static Result Failure(ResultStatus statusCode, IEnumerable<string>? messages = null) => new()
        {
            StatusCode = statusCode,
            Messages = [.. (messages ?? [statusCode.ToString()])]
        };

        public static Result Failure(ResultStatus statusCode, string message) => new()
        {
            StatusCode = statusCode,
            Messages = [message]
        };
    }

    public class Result<T> : ResultBase
    {
        public T Data { get; init; } = default!;

        public static implicit operator Result<T>(Result response)
        {
            return new Result<T>
            {
                Messages = response.Messages,
                Data = default!,
                StatusCode = response.StatusCode
            };
        }
    }
}