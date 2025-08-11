namespace Operum.Service.Integrations.MailSender
{
    public interface IMailSender
    {
        Task<HttpResponseMessage> SendMailConfirmationMail(string userName, string email, string callbackUrl);
    }
}
