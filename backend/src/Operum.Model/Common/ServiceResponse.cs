namespace Operum.Model.Common
{
    public class ServiceResponse
    {
        public string? Message { get; set; }
        public bool? Success { get; set; }
    }

    public class ServiceResponse<T>
    {
        public bool? Success { get; set; }
        public string? Message { get; set; }
        public T Data { get; set; } = default!;

        public static implicit operator ServiceResponse<T>(ServiceResponse response)
        {
            return new ServiceResponse<T> { Success = response.Success, Message = response.Message, Data = default! };
        }
    }
}
