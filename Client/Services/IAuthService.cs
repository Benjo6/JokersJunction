using JokersJunction.Shared.Models;

namespace JokersJunction.Client.Services
{
    public interface IAuthService
    {
        Task<LoginResult> Login(LoginModel loginModel);
        Task Logout();
        Task<RegisterResult> Register(RegisterModel registerModel);
        Task<int> GetBalance();
    }
}
