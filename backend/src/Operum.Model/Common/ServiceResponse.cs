using Operum.Model.Enums;

namespace Operum.Model.Common
{
    public abstract class ServiceResponseBase
    {
        public IReadOnlyCollection<string> Messages { get; init; } = [];
        public StatusCodeEnum StatusCode { get; init; }

        public bool IsSuccess => StatusCode == StatusCodeEnum.Ok;
        public bool IsFailure => !IsSuccess;
    }

    public class ServiceResponse : ServiceResponseBase
    {
        public static ServiceResponse Success(string? message = null) => new()
        {
            StatusCode = StatusCodeEnum.Ok,
            Messages = message != null ? [message] : []
        };

        public static ServiceResponse<T> Success<T>(T data, string? message = null) => new()
        {
            Data = data,
            StatusCode = StatusCodeEnum.Ok,
            Messages = message != null ? [message] : []
        };

        public static ServiceResponse Failure(StatusCodeEnum statusCode, IEnumerable<string>? messages = null) => new()
        {
            StatusCode = statusCode,
            Messages = [.. (messages ?? [statusCode.ToString()])]
        };

        public static ServiceResponse Failure(StatusCodeEnum statusCode, string message) => new()
        {
            StatusCode = statusCode,
            Messages = [message]
        };
    }

    public class ServiceResponse<T> : ServiceResponseBase
    {
        public T Data { get; init; } = default!;

        public static implicit operator ServiceResponse<T>(ServiceResponse response)
        {
            return new ServiceResponse<T>
            {
                Messages = response.Messages,
                Data = default!,
                StatusCode = response.StatusCode
            };
        }
    }
}