using JokersJunction.Shared.Models;
using JokersJunction.Shared.Requests;
using JokersJunction.Shared.Responses;

namespace JokersJunction.Client.Services
{
    public interface IAuthService
    {
        Task<LoginResponse> Login(LoginRequest loginModel);
        Task Logout();
        Task<RegisterResponse> Register(RegisterRequest registerModel);
        Task<int> GetBalance();
    }
}
