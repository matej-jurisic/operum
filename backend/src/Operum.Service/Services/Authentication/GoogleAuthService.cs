using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Operum.Model.DTOs.Auth;
using Operum.Service.Interfaces;

namespace Operum.Service.Services.Authentication
{
    public class GoogleAuthService(IConfiguration configuration) : IGoogleAuthService
    {
        private readonly string? _clientId = configuration["Authentication:Google:ClientId"];

        public bool IsEnabled => !string.IsNullOrWhiteSpace(_clientId);

        public async Task<GoogleUserInfo?> GetUserInfoAsync(string idToken)
        {
            if (!IsEnabled)
                return null;

            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [_clientId]
                });

                if (payload == null)
                    return null;

                return new GoogleUserInfo
                {
                    Email = payload.Email,
                    EmailVerified = payload.EmailVerified,
                    Name = payload.Name,
                    Picture = payload.Picture,
                    GivenName = payload.GivenName,
                    FamilyName = payload.FamilyName,
                    Locale = payload.Locale
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token validation failed: {ex.Message}");
                return null;
            }
        }
    }
}