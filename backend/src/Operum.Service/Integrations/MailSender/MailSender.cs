using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Operum.Service.Integrations.MailSender
{
    public class MailSender(IConfiguration configuration) : IMailSender
    {
        public async Task<HttpResponseMessage> SendMailConfirmationMail(string userName, string email, string callbackUrl)
        {
            var apiUrl = configuration.GetValue<string?>("MailerSend:BaseUrl");
            var apiKey = configuration.GetValue<string?>("MailerSend:ApiKey");
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiUrl))
                throw new Exception("Missing mail sender configuration!");

            var payload = new
            {
                from = new { email = "confirm@operum.app" },
                to = new[]
                {
                    new { email }
                },
                personalization = new[]
                {
                    new
                    {
                        email,
                        data = new
                        {
                            callback_url = callbackUrl,
                            name = userName
                        }
                    }
                },
                subject = "Confirm your email",
                template_id = "0r83ql3j93zgzw1j"
            };

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            return (await client.PostAsync(apiUrl, content));
        }
    }
}
