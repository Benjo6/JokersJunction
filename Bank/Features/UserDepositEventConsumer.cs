using JokersJunction.Common.Databases.Models;
using JokersJunction.Common.Events;
using JokersJunction.Shared.Models;
using MassTransit;
using Microsoft.AspNetCore.Identity;

namespace JokersJunction.Bank.Features;

public class UserDepositEventConsumer : IConsumer<UserDepositEvent>
{
    private readonly UserManager<ApplicationUser> _userManager;
    public UserDepositEventConsumer(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task Consume(ConsumeContext<UserDepositEvent> context)
    {
        var user = await _userManager.FindByNameAsync(context.Message.User.Name);

        if (user.Currency >= context.Message.Amount)
        {
            user.Currency -= context.Message.Amount;
            await _userManager.UpdateAsync(user);
        }
    }
}