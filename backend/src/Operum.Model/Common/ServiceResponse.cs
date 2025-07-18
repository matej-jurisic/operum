using Operum.Model.Enums;

namespace Operum.Model.Common
{
    public class ServiceResponse
    {
        public IEnumerable<string>? Messages { get; set; }
        public StatusCodeEnum StatusCode { get; set; }

        public static ServiceResponse Success(string? message = null) => new() { StatusCode = StatusCodeEnum.Ok, Messages = message != null ? [message] : [] };
        public static ServiceResponse Failure(StatusCodeEnum statusCode, IEnumerable<string>? messages = null) => new() { StatusCode = statusCode, Messages = messages ?? [statusCode.ToString()] };
        public static ServiceResponse Failure(StatusCodeEnum statusCode, string message) => new() { StatusCode = statusCode, Messages = [message] };
        public static ServiceResponse<T> Success<T>(T data, string? message = null) => new() { Data = data, StatusCode = StatusCodeEnum.Ok, Messages = message != null ? [message] : [] };
    }

    public class ServiceResponse<T>
    {
        public IEnumerable<string>? Messages { get; set; }
        public T Data { get; set; } = default!;
        public StatusCodeEnum StatusCode { get; set; }

        public static implicit operator ServiceResponse<T>(ServiceResponse response)
        {
            return new ServiceResponse<T> { Messages = response.Messages, Data = default! };
        }
    }
}
