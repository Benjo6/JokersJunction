using JokersJunction.Common.Databases.Interfaces;
using JokersJunction.Common.Events;
using MassTransit;

namespace JokersJunction.GameUser.Features;

public class UserInGameEventConsumer : IConsumer<UserInGameEvent>
{
    private IDatabaseService _databaseService;

    public UserInGameEventConsumer(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task Consume(ConsumeContext<UserInGameEvent> context)
    {
        var user = context.Message.User;
        user.InGame = true;
        await _databaseService.ReplaceOneAsync(user);
    }
}