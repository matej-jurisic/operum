using Operum.Model.Common;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit.Abstractions;

namespace Operum.Tests.Extensions
{
    public static class ResponseExtensions
    {
        public static async Task PrintMessages(this ITestOutputHelper outputHelper, HttpResponseMessage response)
        {
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (apiResponse != null && apiResponse.Messages != null)
            {
                foreach (var message in apiResponse.Messages)
                {
                    outputHelper.WriteLine($"Register Response Message: {message}");
                }
            }
        }
    }
}
