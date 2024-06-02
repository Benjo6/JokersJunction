using JokersJunction.Common.Events;
using JokersJunction.GameManagement.Services.Contracts;
using MassTransit;

namespace JokersJunction.GameManagement.Features;

public class PlayerStateRefreshEventConsumer : IConsumer<PokerPlayerStateRefreshEvent>, IConsumer<BlackjackPlayerStateRefreshEvent>
{
    private readonly IGameService _gameService;
    public PlayerStateRefreshEventConsumer( IGameService gameService)
    {
        _gameService = gameService;
    }
    public async Task Consume(ConsumeContext<PokerPlayerStateRefreshEvent> context)
    {
        await _gameService.PokerPlayerStateRefresh(context.Message.TableId, context.Message.Games);
    }

    public async Task Consume(ConsumeContext<BlackjackPlayerStateRefreshEvent> context)
    {
        await _gameService.BlackjackPlayerStateRefresh(context.Message.TableId);
    }
}