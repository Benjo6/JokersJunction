using JokersJunction.Common.Events;
using JokersJunction.GameManagement.Services.Contracts;
using JokersJunction.Server.Responses;
using JokersJunction.Shared;
using JokersJunction.Shared.Models;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace JokersJunction.MatchMaker.Features;

public class PlayerStateRefreshEventConsumer : IConsumer<PlayerStateRefreshEvent>
{
    private readonly IGameService _gameService;
    public PlayerStateRefreshEventConsumer( IGameService gameService)
    {
        _gameService = gameService;
    }

    public async Task Consume(ConsumeContext<PlayerStateRefreshEvent> context)
    {
        await _gameService.PlayerStateRefresh(context.Message.TableId, context.Message.Users, context.Message.Games);

    }
}