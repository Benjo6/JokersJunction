using JokersJunction.Common.Databases.Interfaces;
using JokersJunction.Common.Events;
using MassTransit;

namespace JokersJunction.GameUser.Features;

public class UpdateUserEventConsumer : IConsumer<UpdateUserEvent>
{
    private readonly IDatabaseService _databaseService;

    public UpdateUserEventConsumer(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task Consume(ConsumeContext<UpdateUserEvent> context)
    {
        await _databaseService.ReplaceOneAsync(context.Message.GameUser);
    }
}