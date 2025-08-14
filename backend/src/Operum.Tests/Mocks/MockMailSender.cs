using Operum.Service.Integrations.MailSender;
using RestSharp;

namespace Operum.Tests.Mocks
{
    public class MockMailSender : IMailSender
    {
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
