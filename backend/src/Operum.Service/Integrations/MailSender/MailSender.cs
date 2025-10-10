using Microsoft.Extensions.Options;
using Operum.Model.Configuration;
using Operum.Service.Interfaces;
using RestSharp;
using RestSharp.Authenticators;
using System.Text.Json;

namespace Operum.Service.Integrations.MailSender
{
    public class MailSender(IOptions<MailGunConfigurationModel> settings) : IMailSender
    {
        private readonly MailGunConfigurationModel _settings = settings.Value;

        public async Task<RestResponse> SendMailConfirmationMail(string userName, string email, string callbackUrl)
        {
            var options = new RestClientOptions(_settings.ApiBase)
            {
                Authenticator = new HttpBasicAuthenticator("api", _settings.ApiKey)
            };

            var variables = new
            {
                name = userName,
                callback_url = callbackUrl
            };

            using var client = new RestClient(options);
            var request = new RestRequest(_settings.ApiRoute, Method.Post)
            {
                AlwaysMultipartFormData = true
            };

            request.AddParameter("from", "Operum <postmaster@operum.app>");
            request.AddParameter("to", $"{userName} <{email}>");
            request.AddParameter("subject", "Confirm your email address");
            request.AddParameter("template", "mail confirmation");
            request.AddParameter("h:X-Mailgun-Variables", JsonSerializer.Serialize(variables));

            return await client.ExecuteAsync(request);
        }
    }
}