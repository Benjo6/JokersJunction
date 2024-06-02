using JokersJunction.Common.Databases.Models;
using JokersJunction.Server.Events;
using JokersJunction.Server.Events.Response;
using JokersJunction.Table.Repositories.Interfaces;
using MassTransit;

namespace JokersJunction.Table.Features;

public class CurrentPokerTableEventConsumer : IConsumer<CurrentPokerTableEvent>
{
    private readonly ITableRepository _tableRepository;

    public CurrentPokerTableEventConsumer(ITableRepository tableRepository)
    {
        _tableRepository = tableRepository;
    }

    public async Task Consume(ConsumeContext<CurrentPokerTableEvent> context)
    {
        var table = await _tableRepository.GetTableById<PokerTable>(context.Message.TableId) ?? throw new NullReferenceException($"There is no table with this Id:{context.Message.TableId}");

        await context.RespondAsync(new CurrentPokerTableEventResponse
        {
            Table = table
        });
    }
}

public class CurrentBlackjackTableEventConsumer : IConsumer<CurrentBlackjackTableEvent>
{
    private readonly ITableRepository _tableRepository;

    public CurrentBlackjackTableEventConsumer(ITableRepository tableRepository)
    {
        _tableRepository = tableRepository;
    }

    public async Task Consume(ConsumeContext<CurrentBlackjackTableEvent> context)
    {
        var table = await _tableRepository.GetTableById<BlackjackTable>(context.Message.TableId) ?? throw new NullReferenceException($"There is no table with this Id:{context.Message.TableId}");

        await context.RespondAsync(new CurrentBlackjackTableEventResponse()
        {
            Table = table
        });
    }
}