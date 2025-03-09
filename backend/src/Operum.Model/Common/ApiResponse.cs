using Operum.Model.Enums;
using System.Text.Json.Serialization;

namespace Operum.Model.Common
{
    public class ApiResponse
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Data { get; set; }
        public StatusCodeEnum StatusCode { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IEnumerable<string>? Messages { get; set; }
    }
}
