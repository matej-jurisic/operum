using Operum.Model.DTOs.Auth;
using Operum.Service.Interfaces;
using System.Net.Http.Json;

namespace Operum.Service.Services.Authentication
{
    public class GoogleAuthService(HttpClient http) : IGoogleAuthService
    {
        private readonly HttpClient _http = http;

        public async Task<GoogleUserInfo?> GetUserInfoAsync(string idToken)
        {
            var response = await _http.GetFromJsonAsync<GoogleUserInfo>(
                $"https://www.googleapis.com/oauth2/v3/tokeninfo?id_token={idToken}"
            );

            return response;
        }
    }

}