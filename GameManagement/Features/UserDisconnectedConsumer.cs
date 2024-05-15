using JokersJunction.Common.Databases.Interfaces;
using JokersJunction.Common.Databases.Models;
using JokersJunction.Shared;
using JokersJunction.Shared.Events;
using MassTransit;

namespace JokersJunction.GameManagement.Features;

public sealed class UserDisconnectedConsumer : IConsumer<UserDisconnectedEvent>
{
    private IDatabaseService _databaseService;
    public UserDisconnectedConsumer(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task Consume(ConsumeContext<UserDisconnectedEvent> context)
    {
    }
}