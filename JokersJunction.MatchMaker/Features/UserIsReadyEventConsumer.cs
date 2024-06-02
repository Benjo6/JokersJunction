using JokersJunction.Common.Databases.Interfaces;
using JokersJunction.Common.Databases.Models;
using JokersJunction.Common.Events.Responses;
using JokersJunction.Common.Events;
using MassTransit;

namespace JokersJunction.MatchMaker.Features;

public class UserIsReadyEventConsumer : IConsumer<UserIsReadyEvent>
{
    private readonly IDatabaseService _databaseService;

    public UserIsReadyEventConsumer(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task Consume(ConsumeContext<UserIsReadyEvent> context)
    {
        var user = await _databaseService.GetOneByNameAsync<User>(context.Message.Name);
        if (user is not null)
        {
            user.IsReady = true;
            await _databaseService.ReplaceOneAsync(user);
        }

        await context.RespondAsync(new UserIsReadyEventResponse()
        {
            User = user
        });
    }
}