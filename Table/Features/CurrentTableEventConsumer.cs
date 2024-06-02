using JokersJunction.Server.Events;
using JokersJunction.Server.Events.Response;
using JokersJunction.Table.Repositories.Interfaces;
using MassTransit;

namespace JokersJunction.Table.Features;

public class CurrentTableEventConsumer : IConsumer<CurrentTableEvent>
{
    private readonly ITableRepository _tableRepository;

    public CurrentTableEventConsumer(ITableRepository tableRepository)
    {
        _tableRepository = tableRepository;
    }

    public async Task Consume(ConsumeContext<CurrentTableEvent> context)
    {
        var table = await _tableRepository.GetTableById(context.Message.TableId) ?? throw new NullReferenceException($"There is no table with this Id:{context.Message.TableId}");

        await context.RespondAsync(new CurrentTableEventResponse
        {
            Table = table
        });
    }
}