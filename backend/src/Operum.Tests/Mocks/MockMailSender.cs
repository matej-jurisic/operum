using Operum.Service.Interfaces;
using RestSharp;

namespace Operum.Tests.Mocks
{
    public class MockMailSender : IMailSender
    {
        // Stands in for a fully configured Mailgun account, so tests exercise the confirmation flow.
        public bool IsEnabled => true;

        public Task<RestResponse> SendMailConfirmationMail(string userName, string email, string confirmationLink)
        {
            return Task.FromResult(new RestResponse
            {
                IsSuccessStatusCode = true,
                StatusCode = System.Net.HttpStatusCode.OK
            });
        }
    }
}
