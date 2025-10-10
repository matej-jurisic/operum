using RestSharp;

namespace Operum.Service.Interfaces
{
    public interface IMailSender
    {
        Task<RestResponse> SendMailConfirmationMail(string userName, string email, string callbackUrl);
    }
}
