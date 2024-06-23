using JokersJunction.Common.Events;
using JokersJunction.Shared.Models;
using MassTransit;
using Microsoft.AspNetCore.Identity;

namespace JokersJunction.Bank.Features;

public class UserWithdrawEventConsumer : IConsumer<UserWithdrawEvent>
{
    private UserManager<ApplicationUser> _userManager;

    public UserWithdrawEventConsumer(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task Consume(ConsumeContext<UserWithdrawEvent> context)
    {
        var user = await _userManager.FindByNameAsync(context.Message.GameUser.Name ?? context.Message.Name);
            
        if (user != null)
        {
            user.Currency += context.Message.GameUser.Balance;
            await _userManager.UpdateAsync(user);
        }

    }
}