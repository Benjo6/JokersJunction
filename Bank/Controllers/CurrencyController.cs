using Grpc.Core;
using JokersJunction.Bank.Protos;
using JokersJunction.Common.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JokersJunction.Bank.Controllers;

[Route("api/currency")]
[ApiController]
public class CurrencyController : GrpcControllerBase<Currency.CurrencyClient>
{
    private readonly ILogger<CurrencyController> _logger;

    public CurrencyController(ILogger<CurrencyController> logger)
    {
        _logger = logger;
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("add")]
    public async Task<IActionResult> Add(int amount, string userName)
    {
        try
        {
            var response = await Service.AddAsync(new AddRequest { UserName = userName, Amount = amount });
            if (response.Success)
            {
                return Ok(); // Successful response
            }
            else
            {
                return BadRequest(response.ErrorMessage); // Handle error
            }
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "Error adding currency");
            return StatusCode((int)ex.StatusCode, ex.Status.Detail);
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("remove")]
    public async Task<IActionResult> Remove(int amount, string userName)
    {
        try
        {
            var response = await Service.RemoveAsync(new RemoveRequest { UserName = userName, Amount = amount });
            if (response.Success)
            {
                return Ok(); // Successful response
            }
            else
            {
                return BadRequest(response.ErrorMessage); // Handle error
            }
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "Error removing currency");
            return StatusCode((int)ex.StatusCode, ex.Status.Detail);
        }
    }

    //[Authorize("Admin, User")]
    [HttpGet("balance/{userName}")]
    public async Task<IActionResult> Balance(string userName)
    {
        try
        {
            var response = await Service.BalanceAsync(new BalanceRequest { UserName = userName });
            return Ok(response.Balance); // Successful response
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "Error getting balance");
            return StatusCode((int)ex.StatusCode, ex.Status.Detail);
        }
    }
}