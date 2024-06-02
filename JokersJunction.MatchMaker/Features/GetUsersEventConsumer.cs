using JokersJunction.Common.Databases.Interfaces;
using JokersJunction.Common.Databases.Models;
using JokersJunction.Common.Events;
using JokersJunction.Common.Events.Responses;
using MassTransit;

namespace JokersJunction.GameUser.Features;

public class GetUsersEventConsumer : IConsumer<GetUsersEvent>
{
    private readonly IDatabaseService _databaseService;

    public GetUsersEventConsumer(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task Consume(ConsumeContext<GetUsersEvent> context)
    {
        var users = await _databaseService.ReadAsync<User>();

        await context.RespondAsync(new GetUsersEventResponse()
        {
            Users = users
        });
    }
}