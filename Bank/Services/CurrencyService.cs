using Grpc.Core;
using JokersJunction.Bank.Protos;
using JokersJunction.Shared.Models;
using Microsoft.AspNetCore.Identity;

namespace JokersJunction.Bank.Services;
public class CurrencyService : Currency.CurrencyBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public CurrencyService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public override async Task<AddResponse> Add(AddRequest request, ServerCallContext context)
    {
        try
        {
            var user = await _userManager.FindByNameAsync(request.UserName);
            if (user == null)
            {
                // User not found
                return new AddResponse { Success = false, ErrorMessage = "User not found" };
            }

            user.Currency += request.Amount;
            await _userManager.UpdateAsync(user);
            return new AddResponse { Success = true };
        }
        catch (Exception ex)
        {
            // Log the exception
            Console.WriteLine($"Error adding currency: {ex.Message}");
            return new AddResponse { Success = false, ErrorMessage = $"Error adding currency: {ex.Message}" };
        }
    }

    public override async Task<RemoveResponse> Remove(RemoveRequest request, ServerCallContext context)
    {
        try
        {
            var user = await _userManager.FindByNameAsync(request.UserName);
            if (user == null)
            {
                // User not found
                return new RemoveResponse { Success = false, ErrorMessage = "User not found" };
            }

            if (user.Currency >= request.Amount)
            {
                user.Currency -= request.Amount;
                await _userManager.UpdateAsync(user);
                return new RemoveResponse { Success = true };
            }
            else
            {
                // Insufficient balance
                return new RemoveResponse { Success = false, ErrorMessage = "Insufficient balance" };
            }
        }
        catch (Exception ex)
        {
            // Log the exception
            Console.WriteLine($"Error removing currency: {ex.Message}");
            return new RemoveResponse { Success = false, ErrorMessage = $"Error removing currency: {ex.Message}" };
        }
    }

    public override async Task<BalanceResponse> Balance(BalanceRequest request, ServerCallContext context)
    {
        try
        {
            var user = await _userManager.FindByNameAsync(request.UserName);
            var balance = user?.Currency ?? 0;
            return new BalanceResponse { Balance = balance };
        }
        catch (Exception ex)
        {
            // Log the exception
            Console.WriteLine($"Error getting balance: {ex.Message}");
            return new BalanceResponse { Balance = 0, ErrorMessage = $"Error getting balance: {ex.Message}" };
        }
    }
}