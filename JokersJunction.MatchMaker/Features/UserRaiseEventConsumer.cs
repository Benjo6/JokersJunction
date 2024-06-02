using JokersJunction.Common.Databases.Interfaces;
using JokersJunction.Common.Databases.Models;
using JokersJunction.Common.Events;
using MassTransit;

namespace JokersJunction.MatchMaker.Features;

public class UserRaiseEventConsumer : IConsumer<UserRaiseEvent>
{
    private IDatabaseService _databaseService;

    public UserRaiseEventConsumer(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task Consume(ConsumeContext<UserRaiseEvent> context)
    {
        context.Message.GameUser.Balance -= context.Message.Amount;
        await _databaseService.ReplaceOneAsync(context.Message.GameUser);
    }
}