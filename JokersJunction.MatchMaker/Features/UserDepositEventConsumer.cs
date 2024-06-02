using JokersJunction.Common.Databases.Interfaces;
using JokersJunction.Common.Events;
using MassTransit;

namespace JokersJunction.MatchMaker.Features;

public class UserDepositEventConsumer : IConsumer<UserDepositEvent>
{
    private readonly IDatabaseService _databaseService;

    public UserDepositEventConsumer(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task Consume(ConsumeContext<UserDepositEvent> context)
    {
        context.Message.User.Balance = context.Message.Amount;
        await _databaseService.ReplaceOneAsync(context.Message.User);
    }
}