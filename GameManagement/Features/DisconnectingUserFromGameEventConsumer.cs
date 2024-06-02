using JokersJunction.Common.Databases.Interfaces;
using JokersJunction.Common.Databases.Models;
using JokersJunction.Common.Events;
using JokersJunction.Common.Events.Responses;
using JokersJunction.GameManagement.Services.Contracts;
using JokersJunction.Shared;
using MassTransit;
using System.Numerics;

namespace JokersJunction.GameManagement.Features;

public class DisconnectingUserFromGameEventConsumer : IConsumer<DisconnectingUserFromGameEvent>
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
        var pokerGames = await _databaseService.ReadAsync<PokerGame>();
        var game = pokerGames.Find(x => x.TableId == context.Message.TableId);
        if (game != null)
        {

            foreach (var player in game.Players.Where(player => player.Name == context.Message.UserName))
            {
                player.ActionState = PlayerActionState.Left;
            }

            if (game.Players.Count(e => e.ActionState != PlayerActionState.Left) < 2)
            {
                await _gameService.UpdatePot(game);
                await _gameService.GetAndAwardWinners(game);
                await _gameService.PokerPlayerStateRefresh(context.Message.TableId, pokerGames);
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
        else
        {
            var blackjackGames = await _databaseService.ReadAsync<BlackjackGame>();

            var blackjackGame = blackjackGames.FirstOrDefault(e => e.TableId == context.Message.TableId);
            var blackjackPlayer = blackjackGame.Players.FirstOrDefault(p => p.Name == context.Message.UserName);
            if (blackjackPlayer != null)
            {
                blackjackGame.Players.Remove(blackjackPlayer);
                await _databaseService.ReplaceOneAsync(blackjackGame);
                if (blackjackGame.Players.Count == 0)
                {
                    await _databaseService.DeleteOneAsync(blackjackGame);
                }
            }
        }
    }
}
