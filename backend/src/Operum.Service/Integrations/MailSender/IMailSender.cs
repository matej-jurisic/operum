using RestSharp;

namespace Operum.Service.Integrations.MailSender
{
    public interface IMailSender
    {
        Task<RestResponse> SendMailConfirmationMail(string userName, string email, string callbackUrl);
    }
}
