using RestSharp;

namespace Operum.Service.Interfaces
{
    public interface IMailSender
    {
        /// <summary>Whether a Mailgun API key is configured. When false, no mail can be sent.</summary>
        bool IsEnabled { get; }

        Task<RestResponse> SendMailConfirmationMail(string userName, string email, string callbackUrl);
    }
}
