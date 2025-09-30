using Operum.Model.Enums;

namespace Operum.Model.Common
{
    public abstract class ResultBase
    {
        public IReadOnlyCollection<string> Messages { get; init; } = [];
        public StatusCodeEnum StatusCode { get; init; }

        public bool IsSuccess => StatusCode == StatusCodeEnum.Ok;
        public bool IsFailure => !IsSuccess;
    }

    public class Result : ResultBase
    {
        public static Result Success(string? message = null) => new()
        {
            StatusCode = StatusCodeEnum.Ok,
            Messages = message != null ? [message] : []
        };

        public static Result<T> Success<T>(T data, string? message = null) => new()
        {
            Data = data,
            StatusCode = StatusCodeEnum.Ok,
            Messages = message != null ? [message] : []
        };

        public static Result Failure(StatusCodeEnum statusCode, IEnumerable<string>? messages = null) => new()
        {
            StatusCode = statusCode,
            Messages = [.. (messages ?? [statusCode.ToString()])]
        };

        public static Result Failure(StatusCodeEnum statusCode, string message) => new()
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