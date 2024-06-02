using JokersJunction.Common.Databases.Interfaces;
using JokersJunction.Common.Events;
using MassTransit;

namespace JokersJunction.GameUser.Features;

public class UserWithdrawEventConsumer : IConsumer<UserWithdrawEvent>
{
    private IDatabaseService _databaseService;

    public UserWithdrawEventConsumer(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task Consume(ConsumeContext<UserWithdrawEvent> context)
    {
        context.Message.GameUser.Balance = 0;
        await _databaseService.ReplaceOneAsync(context.Message.GameUser);
    }
}