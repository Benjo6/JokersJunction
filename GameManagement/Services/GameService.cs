using JokersJunction.Common.Databases.Interfaces;
using JokersJunction.Common.Databases.Models;
using JokersJunction.Common.Events;
using JokersJunction.Common.Events.Responses;
using JokersJunction.GameManagement.Services.Contracts;
using JokersJunction.Server.Evaluators;
using JokersJunction.Server.Events;
using JokersJunction.Server.Events.Response;
using JokersJunction.Server.Hubs;
using JokersJunction.Server.Responses;
using JokersJunction.Shared;
using JokersJunction.Shared.Models;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Collections;

namespace JokersJunction.GameManagement.Services;

public class GameService : IGameService
{
    private readonly IHubContext<GameHub> _hubContext;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IDatabaseService _databaseService;
    private readonly IRequestClient<CurrentTableEvent> _currentTableRequestClient;
    private readonly IRequestClient<StartBlindEvent> _startBlindRequestClient;
    private readonly IRequestClient<GetUsersEvent> _getUsersRequestClient;
    private readonly IRequestClient<GetUserByNameEvent> _getUserByNameRequestClient;
    public GameService(IHubContext<GameHub> hubContext, IPublishEndpoint publishEndpoint, IDatabaseService databaseService, IRequestClient<CurrentTableEvent> currentTableRequestClient, IRequestClient<StartBlindEvent> startBlindRequestClient, IRequestClient<GetUsersEvent> getUsersRequestClient)
    {
        _hubContext = hubContext;
        _publishEndpoint = publishEndpoint;
        _databaseService = databaseService;
        _currentTableRequestClient = currentTableRequestClient;
        _startBlindRequestClient = startBlindRequestClient;
        _getUsersRequestClient = getUsersRequestClient;
    }

    public async Task PlayerStateRefresh(string tableId, List<Game>? games)
    {
        var playerState = new PlayerStateModel();
        var responseUsers = await _getUsersRequestClient.GetResponse<GetUsersEventResponse>(new GetUsersEvent());
        var users = responseUsers.Message.Users;
        foreach (var user in users.Where(x=>x.TableId == tableId))
        {
            playerState.Players.Add(new GamePlayer
            {
                Username = user.Name,
                IsPlaying = user.InGame,
                IsReady = user.IsReady,
                SeatNumber = user.SeatNumber,
                GameMoney = user.Balance
            });

            if (games.FirstOrDefault(e=>e.TableId==tableId) != null &&
                games.First(e => e.TableId == tableId).Players.Select(e => e.Name).Contains(user.Name))
            {
                playerState.Players.Last().ActionState = games.First(e => e.TableId == tableId).Players
                    .First(e => e.Name == user.Name).ActionState;
            }
        }

        playerState.CommunityCards = games.FirstOrDefault(e => e.TableId == tableId)?.TableCards;
        playerState.GameInProgress = playerState.CommunityCards != null;
        playerState.Pots = games.FirstOrDefault(e => e.TableId == tableId)?.Winnings;
        if (games.FirstOrDefault(e => e.TableId == tableId) != null)
            playerState.SmallBlind = games.First(e => e.TableId == tableId).SmallBlind;

        if (games.FirstOrDefault(e => e.TableId == tableId)?.RaiseAmount > 0)
            playerState.RaiseAmount = games.First(e => e.TableId == tableId).RaiseAmount;

        var playerStateResponse = new PlayerStateResponse()
        {
            GamePlayers = games.FirstOrDefault(e => e.TableId == tableId)?.Players,
            PlayerStateModel = playerState,
            TableUsers = users.Where(e => e.TableId == tableId),
            TableId = tableId
        };
        await _hubContext.Clients.Group(tableId).SendAsync("PlayerStateRefresh", playerStateResponse);
    }

    public async Task StartGame(string tableId, int smallBlindPosition, List<User> users)
    {
        // Initialize Game
        var responseTable = await _currentTableRequestClient.GetResponse<CurrentTableEventResponse>(new CurrentTableEvent()
        {
            TableId = tableId
        });
        var currentTableInfo = responseTable.Message.Table;

        var newGame = new Game(tableId, smallBlindPosition, currentTableInfo.SmallBlind);

        // Adding players to table
        foreach (var user in users.Where(u=> u.IsReady && u.TableId == tableId))
        {
            newGame.Players.Add(new Player{Name = user.Name, RoundBet = 0});
            await _publishEndpoint.Publish(new UserInGameEvent
            {
                User = user
            });
        }

        newGame.NormalizeAllIndexes();

        // Small blind
        var responseBlind = await _startBlindRequestClient.GetResponse<StartBlindEventResponse>(new StartBlindEvent
        {
            SmallBlindName = newGame.GetPlayerNameByIndex(newGame.SmallBlindIndex),
            SmallBlind = newGame.SmallBlind,
            BigBlindName = newGame.GetPlayerNameByIndex(newGame.BigBlindIndex),
            BigBlind = newGame.SmallBlind * 2,
            Players = newGame.Players,
        });
        newGame.Players.First(e => e.Name == newGame.GetPlayerNameByIndex(newGame.SmallBlindIndex)).RoundBet =
            responseBlind.Message.SmallRoundBet;
        newGame.Players.First(e => e.Name == newGame.GetPlayerNameByIndex(newGame.BigBlindIndex)).RoundBet =
            responseBlind.Message.BigRoundBet;

        var responseUsers = await _getUsersRequestClient.GetResponse<GetUsersEventResponse>(new GetUsersEvent());
        users = responseUsers.Message.Users;

        //Deal cards
        foreach (var player in newGame.Players)
        {
            if (users.Where(e => e.TableId == tableId && e.InGame).Select(e => e.Name).ToList().Contains(player.Name))
            {
                player.HandCards.AddRange(newGame.Deck.DrawCards(2));
                var connectionId = users.First(e => e.Name == player.Name).ConnectionId;
                if (users.First(e => e.Name == player.Name).InGame)
                    await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveStartingHand", player.HandCards);
            }
        }

        _databaseService.InsertOne(newGame);

        var games = await _databaseService.ReadAsync<Game>();

        //Notify about the end of preparations
        await PlayerStateRefresh(tableId, games);

        await _hubContext.Clients.Group(tableId)
            .SendAsync("ReceiveTurnPlayer", newGame.GetPlayerNameByIndex(newGame.Index));
    }

    public async Task MoveIndex(string tableId, Game currentGame)
    {
        do
        {
            currentGame.SetIndex(currentGame.Index+1);
            await _databaseService.ReplaceOneAsync(currentGame);
            if (currentGame.Index == currentGame.RoundEndIndex)
            {
                await CommunityCardsController(currentGame);
                await UpdatePot(currentGame);
                currentGame.RaiseAmount = 0;
                currentGame.RoundEndIndex = currentGame.BigBlindIndex + 1;
                currentGame.Index = currentGame.BigBlindIndex + 1;
                currentGame.NormalizeAllIndexes();
                foreach (var player in currentGame.Players)
                {
                    player.RoundBet = 0;
                }
                await _databaseService.ReplaceOneAsync(currentGame);
            }
        } while ((currentGame.GetPlayerByIndex(currentGame.Index).ActionState != PlayerActionState.Playing || (await _databaseService.GetOneByNameAsync<User>(currentGame.GetPlayerByIndex(currentGame.Index).Name)).Balance == 0
                     || currentGame.Players.Count(e => e.ActionState == PlayerActionState.Playing) < 2) && currentGame.CommunityCardsActions !=
                 CommunityCardsActions.AfterRiver);

        var responseUser = await _getUsersRequestClient.GetResponse<GetUsersEventResponse>(new GetUsersEvent());
        var users = responseUser.Message.Users;
        var games = await _databaseService.ReadAsync<Game>();

        if (currentGame.CommunityCardsActions == CommunityCardsActions.AfterRiver)
        {
            Thread.Sleep(10000);

            await _databaseService.DeleteOneAsync(currentGame);

            games = await _databaseService.ReadAsync<Game>();
            if (users.Where(e => e.TableId == tableId).Count(e => e.IsReady) >= 2 && games.All(e => e.TableId != tableId))
            {
                await StartGame(tableId, currentGame.SmallBlindIndex + 1, users);
            }
            else
            {
                foreach (var e in users.Where(e => e.TableId == tableId))
                {
                    e.InGame = false;
                    await _databaseService.ReplaceOneAsync(e);
                }
                await PlayerStateRefresh(tableId, games);
            }
        }
        else
        {
            await PlayerStateRefresh(tableId, games);
            await _hubContext.Clients.Group(tableId)
                .SendAsync("ReceiveTurnPlayer",
                    currentGame.GetPlayerNameByIndex(currentGame.Index));
        }
    }

    public async Task CommunityCardsController(Game currentGame)
    {
        var responseUser = await _getUsersRequestClient.GetResponse<GetUsersEventResponse>(new GetUsersEvent());
        var users = responseUser.Message.Users;
        var games = await _databaseService.ReadAsync<Game>();
        
        switch (currentGame.CommunityCardsActions)
        {
            case CommunityCardsActions.PreFlop:
                var flop = currentGame.Deck.DrawCards(3);
                currentGame.TableCards.AddRange(flop);
                currentGame.CommunityCardsActions++;
                await _databaseService.ReplaceOneAsync(currentGame);
                await _hubContext.Clients.Group(currentGame.TableId)
                    .SendAsync("ReceiveFlop", flop);
                break;

            case CommunityCardsActions.Flop:
                var turn = currentGame.Deck.DrawCards(1);
                currentGame.TableCards.AddRange(turn);
                currentGame.CommunityCardsActions++;
                await _databaseService.ReplaceOneAsync(currentGame);
                await _hubContext.Clients.Group(currentGame.TableId)
                    .SendAsync("ReceiveTurnOrRiver", turn);
                break;

            case CommunityCardsActions.Turn:
                var river = currentGame.Deck.DrawCards(1);
                currentGame.TableCards.AddRange(river);
                currentGame.CommunityCardsActions++;
                await _databaseService.ReplaceOneAsync(currentGame);
                await _hubContext.Clients.Group(currentGame.TableId)
                    .SendAsync("ReceiveTurnOrRiver", river);
                break;

            case CommunityCardsActions.River:
                await GetAndAwardWinners(currentGame);
                await PlayerStateRefresh(currentGame.TableId, games);
                currentGame.CommunityCardsActions++;
                await _databaseService.ReplaceOneAsync(currentGame);
                break;
        }
    }

    public async Task GetAndAwardWinners(Game currentGame)
    {
        var communityCards = currentGame.TableCards;
        var evaluatedPlayers = new Hashtable();

        foreach (var player in currentGame.Players.Where(e => e.ActionState == PlayerActionState.Playing))
        {
            player.HandStrength = HandEvaluation.Evaluate(communityCards.Concat(player.HandCards).ToList());
            evaluatedPlayers.Add(player.Name, player.HandStrength);
        }

        foreach (var pot in currentGame.Winnings)
        {
            var highestHand = HandStrength.Nothing;
            string winner = null;
            foreach (var potPlayer in pot.Players.Where(potPlayer =>
                         highestHand > (HandStrength)evaluatedPlayers[potPlayer]))
            {
                highestHand = (HandStrength)evaluatedPlayers[potPlayer];
                winner = potPlayer;
            }

            pot.Winner = winner;
            var winnerUser = await _getUserByNameRequestClient.GetResponse<GetUserByNameEventResponse>(new GetUserByNameEvent()
            {
                Name = pot.Winner
            });
            winnerUser.Message.GameUser.Balance += pot.PotAmount;
            await _publishEndpoint.Publish(new UpdateUserEvent
            {
                GameUser = winnerUser.Message.GameUser
            });
        }
    }

    public async Task UpdatePot(Game currentGame)
    {
        var players = currentGame.Players.Where(player => player.RoundBet > 0 && player.ActionState == PlayerActionState.Playing).ToList();

        while (players.Any())
        {
            var pot = new Pot { PotAmount = players.Min(e => e.RoundBet) };

            foreach (var player in players)
            {
                player.RoundBet -= pot.PotAmount;
                pot.Players.Add(player.Name);
            }

            pot.PotAmount *= players.Count;

            if (currentGame.Winnings.Count(winningPot => winningPot.Players.SetEquals(pot.Players)) > 0)
            {
                currentGame.Winnings.First(e => e.Players.SetEquals(pot.Players)).PotAmount += pot.PotAmount;
            }
            else
            {
                currentGame.Winnings.Add(pot);
            }
            players = players.Where(e => e.RoundBet > 0).ToList();

            await _databaseService.ReplaceOneAsync(currentGame);


        }
    }

}