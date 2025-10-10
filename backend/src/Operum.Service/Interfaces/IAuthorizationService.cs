namespace Operum.Service.Interfaces
{
    public interface IAuthorizationService
    {
        Task<bool> HasRole(string role);
    }
}
