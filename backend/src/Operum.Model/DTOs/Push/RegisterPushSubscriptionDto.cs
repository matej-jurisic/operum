namespace Operum.Model.DTOs.Push
{
    public class RegisterPushSubscriptionDto
    {
        public string Endpoint { get; set; } = string.Empty;
        public string P256dh { get; set; } = string.Empty;
        public string Auth { get; set; } = string.Empty;
    }

    public class UnregisterPushSubscriptionDto
    {
        public string Endpoint { get; set; } = string.Empty;
    }
}
