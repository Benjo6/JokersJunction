using JokersJunction.Authentication.Protos;
using JokersJunction.Common.Controllers;
using Microsoft.AspNetCore.Mvc;
using LoginRequest = JokersJunction.Shared.Requests.LoginRequest;

namespace JokersJunction.Authentication.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthenticationController : GrpcControllerBase<Authorizer.AuthorizerClient>
{
    [HttpPost]
    [Route("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var response = await Service.LoginAsync(new() { Password = request.Password, Email = request.Email, RememberMe = request.RememberMe });
        return Ok(response);
    }

    [HttpPost]
    [Route("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var response = await Service.RegisterAsync(new() { Email = request.Email, Password = request.Password, ConfirmPassword = request.ConfirmPassword });
        return Ok(response);
    }


}