using Microsoft.Extensions.Configuration;
using RestSharp;
using RestSharp.Authenticators;
using System.Text.Json;

namespace Operum.Service.Integrations.MailSender
{
    public class MailSender(IConfiguration configuration) : IMailSender
    {
        public async Task<RestResponse> SendMailConfirmationMail(string userName, string email, string callbackUrl)
        {
            var apiKey = configuration.GetValue<string?>("Mailgun:ApiKey");
            var apiBase = configuration.GetValue<string?>("Mailgun:ApiBase");
            var apiRoute = configuration.GetValue<string?>("Mailgun:ApiRoute");
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiBase) || string.IsNullOrWhiteSpace(apiRoute))
                throw new Exception("Missing Mailgun configuration!");

            var options = new RestClientOptions(apiBase)
            {
                Authenticator = new HttpBasicAuthenticator("api", apiKey)
            };

            var variables = new
            {
                name = userName,
                callback_url = callbackUrl
            };

            var client = new RestClient(options);
            var request = new RestRequest(apiRoute, Method.Post)
            {
                AlwaysMultipartFormData = true
            };
            request.AddParameter("from", "Mailgun Sandbox <postmaster@operum.app>");
            request.AddParameter("to", $"{userName} <{email}>");
            request.AddParameter("subject", "Mail confirmation");
            request.AddParameter("template", "mail confirmation");
            request.AddParameter("h:X-Mailgun-Variables", JsonSerializer.Serialize(variables));
            return await client.ExecuteAsync(request);
        }
    }
}
