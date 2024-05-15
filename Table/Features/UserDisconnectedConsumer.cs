using JokersJunction.Common.Databases.Interfaces;
using JokersJunction.Common.Databases.Models;
using JokersJunction.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace JokersJunction.Table.Features;

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