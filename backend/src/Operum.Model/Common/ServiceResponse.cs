using Operum.Model.Enums;

namespace Operum.Model.Common
{
    public class ServiceResponse
    {
        public IEnumerable<string>? Messages { get; set; }
        public StatusCodeEnum StatusCode { get; set; }

        public static ServiceResponse Success() => new() { StatusCode = StatusCodeEnum.Ok, Messages = ["Success!"] };
        public static ServiceResponse Failure(StatusCodeEnum statusCode, IEnumerable<string>? messages = null) => new() { StatusCode = statusCode, Messages = messages };
        public static ServiceResponse<T> Success<T>(T data) => new() { Data = data, StatusCode = StatusCodeEnum.Ok, Messages = ["Success!"] };
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
