using JokersJunction.Common.Databases.Interfaces;
using JokersJunction.Common.Databases.Models;
using JokersJunction.Common.Events;
using JokersJunction.Common.Events.Responses;
using JokersJunction.GameManagement.Services.Contracts;
using JokersJunction.Shared;
using MassTransit;

namespace JokersJunction.GameManagement.Features;

public class DisconnectingUserFromGameEventConsumer: IConsumer<DisconnectingUserFromGameEvent>
{
    private readonly IDatabaseService _databaseService;
    private IGameService _gameService;

    public DisconnectingUserFromGameEventConsumer(IDatabaseService databaseService, IGameService gameService)
    {
        _databaseService = databaseService;
        _gameService = gameService;
    }

    public async Task Consume(ConsumeContext<DisconnectingUserFromGameEvent> context)
    {
        var games = await _databaseService.ReadAsync<Game>();
        var game = games.Find(x => x.TableId == context.Message.TableId);
        foreach (var player in game.Players.Where(player => player.Name == context.Message.UserName))
        {
            player.ActionState = PlayerActionState.Left;
        }

        if (game.Players.Count(e => e.ActionState != PlayerActionState.Left) < 2)
        {
            await _gameService.UpdatePot(game);
            await _gameService.GetAndAwardWinners(game);
            await _gameService.PlayerStateRefresh(context.Message.TableId,games);
            var smallBlindIndexTemp = game.SmallBlindIndex;
            await _databaseService.ReplaceOneAsync(game);
            await _databaseService.DeleteOneAsync(game);
            Thread.Sleep(10000);

            await context.RespondAsync(new DisconnectingUserFromGameEventResponse()
            {
                SmallBlindIndex = smallBlindIndexTemp,
            });
        }
    }
}