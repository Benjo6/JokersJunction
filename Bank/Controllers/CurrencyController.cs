using Grpc.Core;
using JokersJunction.Bank.Protos;
using JokersJunction.Common.Controllers;
using JokersJunction.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace JokersJunction.Bank.Controllers;

[Route("api/currency")]
[ApiController]
public class CurrencyController : GrpcControllerBase<Currency.CurrencyClient>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public CurrencyController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpPost("add")]
    public async Task<IActionResult> Add(int amount, string userName)
    {
        try
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                // User not found
                return NotFound(new AddResponse { Success = false, ErrorMessage = "User not found" });
            }

            user.Currency += amount;
            await _userManager.UpdateAsync(user);
            return Ok( new AddResponse { Success = true });
        }
        catch (Exception ex)
        {
            // Log the exception
            Console.WriteLine($"Error adding currency: {ex.Message}");
            return BadRequest(new AddResponse { Success = false, ErrorMessage = $"Error adding currency: {ex.Message}" });
        }
    }

    [HttpPost("remove")]
    public async Task<IActionResult> Remove(int amount, string userName)
    {
        try
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                // User not found
                return NotFound(new RemoveResponse { Success = false, ErrorMessage = "User not found" });
            }

            if (user.Currency >= amount)
            {
                user.Currency -= amount;
                await _userManager.UpdateAsync(user);
                return Ok(new RemoveResponse { Success = true });
            }
            else
            {
                // Insufficient balance
                return BadRequest(new RemoveResponse { Success = false, ErrorMessage = "Insufficient balance" });
            }
        }
        catch (Exception ex)
        {
            // Log the exception
            Console.WriteLine($"Error removing currency: {ex.Message}");
            return BadRequest(new RemoveResponse { Success = false, ErrorMessage = $"Error removing currency: {ex.Message}" });
        }
    }

    //[Authorize("Admin, User")]
    [HttpGet("balance/{userName}")]
    public async Task<IActionResult> Balance(string userName)
    {
        try
        {
            var user = await _userManager.FindByNameAsync(userName);
            var balance = user?.Currency ?? 0;
            return Ok(new BalanceResponse { Balance = balance });
        }
        catch (Exception ex)
        {
            // Log the exception
            Console.WriteLine($"Error getting balance: {ex.Message}");
            return BadRequest(new BalanceResponse { Balance = 0, ErrorMessage = $"Error getting balance: {ex.Message}" });
        }
    }
}