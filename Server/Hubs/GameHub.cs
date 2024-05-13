﻿using System.Collections;
using JokersJunction.Common.Databases.Interfaces;
using JokersJunction.Common.Databases.Models;
using JokersJunction.Server.Evaluators;
using JokersJunction.Shared;
using JokersJunction.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using PokerTable = JokersJunction.Common.Databases.Models.PokerTable;

namespace JokersJunction.Server.Hubs;

public class GameHub : Hub
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<GameHub> _logger;


    public GameHub(UserManager<ApplicationUser> userManager, IDatabaseService databaseService, ILogger<GameHub> logger)
    {
        _userManager = userManager;
        _databaseService = databaseService;
        _logger = logger;
    }

    public async Task SendMessage(string message)
    {
        try
        {
            var username = Context.User.Identity.Name;
            if (!await IsUserInGameHub(username)) return;

            var newMessage = new GetMessageResult { Sender = username, Message = message };
            var tableId = await GetTableByUser(username);

            await Clients.Groups(tableId).SendAsync("ReceiveMessage", newMessage);
        }
        catch (Exception ex)
        {
            // Log the exception...
            _logger.LogError(ex, "An error occurred while sending a message.");
        }
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        await DisconnectPlayer(Context.User.Identity.Name);
        await base.OnDisconnectedAsync(exception);
    }

    private async Task DisconnectPlayer(string username)
    {
        try
        {
            var user = await _databaseService.GetOneByNameAsync<User>(username);
            if (user is null)
            {
                return;
            }
            var games = await _databaseService.ReadAsync<Game>();
            var tableId = user.TableId;
            var game = games.First(x => x.TableId == tableId);

            var smallBlindIndexTemp = 0;

            if (user.InGame)
            {
                var player = game.Players.SingleOrDefault(doc => doc.Name == username);
                if (player is null)
                {
                    throw new NullReferenceException($"{username} as a player is null");
                }

                player.ActionState = PlayerActionState.Left;

                if (game.Players.Count(e => e.ActionState != PlayerActionState.Left) < 2)
                {
                    await UpdatePot(tableId);
                    await GetAndAwardWinners(tableId);
                    await PlayerStateRefresh(tableId);
                    smallBlindIndexTemp = game.SmallBlindIndex;
                    await _databaseService.DeleteOneAsync(game);
                    Thread.Sleep(10000);
                }
            }

            await Withdraw(username);
            await _databaseService.DeleteOneAsync(user);
            var users = await _databaseService.ReadAsync<User>();
            foreach (var e in users.Where(e => e.TableId == tableId))
            {
                e.InGame = false;
                await _databaseService.ReplaceOneAsync(e);
            }
            users = await _databaseService.ReadAsync<User>();
            if (users.Count(e => e.IsReady) >= 2 && games.All(e => e.TableId != tableId))
            {
                await StartGame(tableId, smallBlindIndexTemp + 1);
            }
            else
            {
                await PlayerStateRefresh(tableId);
            }
        }
        catch (Exception)
        {
            //Log
        }
    }

    public async Task AddToUsers(string tableId)
    {
        var username = Context.User.Identity.Name;
        var users = await _databaseService.ReadAsync<User>();
        var currentTable = await _databaseService.GetOneFromIdAsync<PokerTable>(tableId);


        if (users.Count(e => e.TableId == tableId) >= currentTable.MaxPlayers)
        {
            await Clients.Client(Context.ConnectionId).SendAsync("ReceiveKick");
            return;
        }

        if (users.Any(e => e.Name == username))
        {
            await Clients.Client(Context.ConnectionId).SendAsync("ReceiveKick");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, tableId);

        var newSeatNumber = await AssignTableToUser(tableId);

        _databaseService.InsertOne(new User
        {
            Name = username,
            TableId = tableId,
            ConnectionId = Context.ConnectionId,
            SeatNumber = newSeatNumber,
            InGame = false,
            Balance = 0
        });
        await PlayerStateRefresh(tableId);
    }

    private async Task<int> AssignTableToUser(string tableId)
    {
        var users = await _databaseService.ReadAsync<User>();
        var occupiedSeats = users.Where(e => e.TableId == tableId).Select(e => e.SeatNumber).OrderBy(e => e).ToList();
        for (var i = 0; i < occupiedSeats.Count; i++)
        {
            if (occupiedSeats[i] != i)
                return i;
        }
        return occupiedSeats.Count;

    }

    public async Task MarkReady(int depositAmount)
    {
        var userName = Context.User.Identity.Name;
        var users = await _databaseService.ReadAsync<User>();
        var games = await _databaseService.ReadAsync<Game>();
        var user = await _databaseService.GetOneByNameAsync<User>(userName);
        if (user is null)
        {
            return;
        }

        if (!await IsUserInGameHub(userName)) return;

        var tableId = await GetTableByUser(userName);

        await Deposit(depositAmount, user);

        user.IsReady = true;

        await _databaseService.ReplaceOneAsync(user);

        await PlayerStateRefresh(tableId);
        users = await _databaseService.ReadAsync<User>();
        if (users.Where(e => e.TableId == tableId).Count(e => e.IsReady) >= 2 && games.All(e => e.TableId != tableId))
        {
            await StartGame(tableId, 0);
        }
    }

    private async Task Deposit(int depositAmount, User userInGame)
    {
        var user = await _userManager.FindByNameAsync(userInGame.Name);

        if (user is not null && user.Currency >= depositAmount)
        {
            user.Currency -= depositAmount;
            await _userManager.UpdateAsync(user);
            userInGame.Balance = depositAmount;
        }
    }

    public async Task UnmarkReady()
    {
        var userName = Context.User.Identity.Name;
        if (userName is null)
        {
            return;
        }
        if (!await IsUserInGameHub(userName)) return;

        var user = await _databaseService.GetOneByNameAsync<User>(userName);
        if (user is null) return;

        await Withdraw(userName);
        user.IsReady = false;
        await _databaseService.ReplaceOneAsync(user);

        await PlayerStateRefresh(await GetTableByUser(userName));
    }

    private async Task Withdraw(string userName)
    {
        var user = await _userManager.FindByNameAsync(userName);
        var userInGame = await _databaseService.GetOneByNameAsync<User>(userName);
        if (user is null || userInGame is null)
        {
            throw new NullReferenceException("Withdraw of currency is not possible, please contact us");
        }
        user.Currency += userInGame.Balance;
        await _userManager.UpdateAsync(user);
        userInGame.Balance = 0;
        await _databaseService.ReplaceOneAsync(userInGame);
    }

    private async Task StartGame(string tableId, int smallBlindPosition)
    {
        //Initialize Game
        var users = await _databaseService.ReadAsync<User>();
        var currentTableInfo = await _databaseService.GetOneFromIdAsync<PokerTable>(tableId);

        var newGame = new Game(tableId, smallBlindPosition, currentTableInfo.SmallBlind);

        //Adding players to table
        foreach (var item in users.Where(user => user.IsReady && user.TableId == tableId))
        {
     
            newGame.Players.Add(new Player { Name = item.Name, RoundBet = 0 });
            item.InGame = true;
        }

        newGame.NormalizeAllIndexes();

        //Small blind
        if (users.First(e => e.Name == newGame.GetPlayerNameByIndex(newGame.SmallBlindIndex)).Balance >=
            newGame.SmallBlind)
        {
            users.First(e => e.Name == newGame.GetPlayerNameByIndex(newGame.SmallBlindIndex)).Balance -=
                newGame.SmallBlind;
            newGame.Players.First(e => e.Name == newGame.GetPlayerNameByIndex(newGame.SmallBlindIndex)).RoundBet +=
                newGame.SmallBlind;
        }
        else
        {
            newGame.Players.First(e => e.Name == newGame.GetPlayerNameByIndex(newGame.SmallBlindIndex)).RoundBet +=
                users.First(e => e.Name == newGame.GetPlayerNameByIndex(newGame.SmallBlindIndex)).Balance;
            users.First(e => e.Name == newGame.GetPlayerNameByIndex(newGame.SmallBlindIndex)).Balance = 0;
        }

        //Big blind
        if (users.First(e => e.Name == newGame.GetPlayerNameByIndex(newGame.BigBlindIndex)).Balance >=
            newGame.SmallBlind * 2)
        {
            users.First(e => e.Name == newGame.GetPlayerNameByIndex(newGame.BigBlindIndex)).Balance -=
                newGame.SmallBlind * 2;
            newGame.Players.First(e => e.Name == newGame.GetPlayerNameByIndex(newGame.BigBlindIndex)).RoundBet +=
                newGame.SmallBlind * 2;
        }
        else
        {
            newGame.Players.First(e => e.Name == newGame.GetPlayerNameByIndex(newGame.BigBlindIndex)).RoundBet +=
                users.First(e => e.Name == newGame.GetPlayerNameByIndex(newGame.BigBlindIndex)).Balance;
            users.First(e => e.Name == newGame.GetPlayerNameByIndex(newGame.BigBlindIndex)).Balance = 0;
        }

        //Deal cards
        foreach (var player in newGame.Players)
        {
            if (users.Where(e => e.TableId == tableId && e.InGame).Select(e => e.Name).ToList().Contains(player.Name))
            {
                player.HandCards.AddRange(newGame.Deck.DrawCards(2));
                var connectionId = users.First(e => e.Name == player.Name).ConnectionId;
                if (users.First(e => e.Name == player.Name).InGame)
                    await Clients.Client(connectionId).SendAsync("ReceiveStartingHand", player.HandCards);
            }
        }

        _databaseService.InsertOne(newGame);
        //Notify about the end of preparations

        await PlayerStateRefresh(tableId);

        await Clients.Group(tableId)
            .SendAsync("ReceiveTurnPlayer", newGame.GetPlayerNameByIndex(newGame.Index));
    }

    public async Task ActionFold()
    {
        var userName = Context.User.Identity.Name;
        if (userName is null)
        {
            return;
        }
        if (!await IsUserInGameHub(userName)) return;
        var tableId = await GetTableByUser(userName);
        var games = await _databaseService.ReadAsync<Game>();
        var currentGame = games.First(e => e.TableId == tableId);

        if (currentGame.Players.Any() &&
            currentGame.GetPlayerNameByIndex(currentGame.Index) == userName)
        {
            //PlayerFolded
            currentGame.Players
                .First(e => e.Name == userName).ActionState = PlayerActionState.Folded;

            //Remove from pots
            foreach (var pot in currentGame.Winnings)
            {
                pot.Players.Remove(userName);
            }

            //CheckIfOnlyOneLeft
            if (currentGame.Players
                    .Count(e => e.ActionState == PlayerActionState.Playing) == 1)
            {
                await UpdatePot(tableId);
                await GetAndAwardWinners(tableId);
                await PlayerStateRefresh(tableId);

                Thread.Sleep(10000);

                await _databaseService.DeleteOneAsync(currentGame);
                var users = await _databaseService.ReadAsync<User>();
                games = await _databaseService.ReadAsync<Game>();
                if (users.Where(e => e.TableId == tableId).Count(e => e.IsReady) >= 2 && games.All(e => e.TableId != tableId))
                {
                    await StartGame(tableId, currentGame.SmallBlindIndex + 1);
                }
                else
                {
                    foreach (var e in users.Where(e => e.TableId == tableId))
                    {
                        e.InGame = false;
                        await _databaseService.ReplaceOneAsync(e);
                    }
                    await PlayerStateRefresh(tableId);
                }
            }
            else
            {
                await MoveIndex(tableId, currentGame);
            }
        }
    }

    public async Task ActionCheck()
    {
        var userName = Context.User.Identity.Name;
        if (userName is null)
        {
            return;
        }
        if (!await IsUserInGameHub(userName)) return;
        var tableId = await GetTableByUser(userName);

        var games = await _databaseService.ReadAsync<Game>();
        var currentGame = games.First(e => e.TableId == tableId);

        if (currentGame.Players.Any() &&
            currentGame.GetPlayerNameByIndex(currentGame.Index) == userName)
        {

            await MoveIndex(tableId, currentGame);
        }
    }

    public async Task ActionRaise(int raiseAmount)
    {
        var userName = Context.User.Identity.Name;
        if (userName is null)
        {
            return;
        }
        var user = await _databaseService.GetOneByNameAsync<User>(userName) ?? throw new NullReferenceException("There is no user with this username");

        if (!await IsUserInGameHub(userName)) return;
        var tableId = await GetTableByUser(userName);

        var games = await _databaseService.ReadAsync<Game>();
        var currentGame = games.First(e => e.TableId == tableId);
        var currentPlayer = currentGame.Players.First(e => e.Name == userName);

        if (currentGame.Players.Any() &&
            currentGame.GetPlayerNameByIndex(currentGame.Index) == userName &&
            user.Balance > raiseAmount + currentGame.RaiseAmount - currentPlayer.RoundBet)
        {
            user.Balance -= raiseAmount + currentGame.RaiseAmount - currentPlayer.RoundBet;

            currentGame.Players.First(e => e.Name == userName).RoundBet = raiseAmount + currentGame.RaiseAmount;

            currentGame.RaiseAmount += raiseAmount;

            currentGame.RoundEndIndex =
                currentGame.Index;

            await _databaseService.ReplaceOneAsync(currentGame);
            await _databaseService.ReplaceOneAsync(user);
            await MoveIndex(tableId, currentGame);
        }
    }

    public async Task ActionCall()
    {
        var userName = Context.User.Identity.Name;
        if (userName is null)
        {
            return;
        }
        var user = await _databaseService.GetOneByNameAsync<User>(userName) ?? throw new NullReferenceException("No player with this username");

        if (!await IsUserInGameHub(userName)) return;
        var tableId = await GetTableByUser(userName);

        var games = await _databaseService.ReadAsync<Game>();
        var currentGame = games.First(e => e.TableId == tableId);
        var currentPlayer = currentGame.Players.First(e => e.Name == userName);

        if (currentGame.Players.Any() &&
            currentGame.GetPlayerNameByIndex(currentGame.Index) == userName &&
            currentGame.RaiseAmount > 0)
        {
            if (currentGame.RaiseAmount - currentPlayer.RoundBet <
                user.Balance)
            {
                user.Balance -= currentGame.RaiseAmount - currentPlayer.RoundBet;
                currentGame.Players.First(e => e.Name == userName).RoundBet = currentGame.RaiseAmount;
            }
            else if (currentGame.RaiseAmount - currentPlayer.RoundBet >=
                     user.Balance)
            {
                var allInSum = user.Balance;
                user.Balance = 0;
                games.First(e => e.TableId == tableId).Players.First(e => e.Name == Context.User.Identity.Name).RoundBet = allInSum;
            }

            await _databaseService.ReplaceOneAsync(currentGame);
            await _databaseService.ReplaceOneAsync(user);
            await MoveIndex(tableId, currentGame);

        }
    }

    public async Task ActionAllIn()
    {
        var userName = Context.User.Identity.Name;
        if (userName is null)
        {
            return;
        }
        var user = await _databaseService.GetOneByNameAsync<User>(userName) ?? throw new NullReferenceException("There is no user with this username");

        if (!await IsUserInGameHub(userName)) return;
        var tableId = await GetTableByUser(userName);
        var games = await _databaseService.ReadAsync<Game>();
        var currentGame = games.First(e => e.TableId == tableId);
        var currentPlayer = currentGame.Players.First(e => e.Name == userName);
            
        if (currentGame.Players.Any() &&
            currentGame.GetPlayerNameByIndex(currentGame.Index) == userName &&
            user.Balance > currentGame.RaiseAmount - currentPlayer.RoundBet)
        {
            var allInSum = user.Balance;
            user.Balance = 0;
            currentGame.Players.First(e => e.Name == userName).RoundBet = allInSum;
            currentGame.RaiseAmount += allInSum;

            currentGame.RoundEndIndex =
                currentGame.Index;

            await MoveIndex(tableId, currentGame);
        }
    }

    private async Task MoveIndex(string tableId, Game currentGame)
    {
        var users = await _databaseService.ReadAsync<User>();
        do
        {
            currentGame.SetIndex(currentGame.Index + 1);

            if (currentGame.Index == currentGame.RoundEndIndex)
            {
                await CommunityCardsController(tableId);
                await UpdatePot(tableId);
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
        } while ((currentGame.GetPlayerByIndex(currentGame.Index).ActionState != PlayerActionState.Playing || users.First(e => e.Name == currentGame.GetPlayerByIndex(currentGame.Index).Name).Balance == 0
                     || currentGame.Players.Count(e => e.ActionState == PlayerActionState.Playing) < 2) && currentGame.CommunityCardsActions !=
                 CommunityCardsActions.AfterRiver);

        if (currentGame.CommunityCardsActions ==
            CommunityCardsActions.AfterRiver)
        {
            Thread.Sleep(10000);

            await _databaseService.DeleteOneAsync(currentGame);
            var games = await _databaseService.ReadAsync<Game>();

            if (users.Where(e => e.TableId == tableId).Count(e => e.IsReady) >= 2 && games.All(e => e.TableId != tableId))
            {
                await StartGame(tableId, currentGame.SmallBlindIndex + 1);
            }
            else
            {
                foreach (var e in users.Where(e => e.TableId == tableId))
                {
                    e.InGame = false;
                    await _databaseService.ReplaceOneAsync(e);
                }
                await PlayerStateRefresh(tableId);
            }
        }
        else
        {
            await PlayerStateRefresh(tableId);
            await Clients.Group(tableId)
                .SendAsync("ReceiveTurnPlayer",
                    currentGame.GetPlayerNameByIndex(currentGame.Index));
        }
    }

    public async Task CommunityCardsController(string tableId)
    {
        var games = await _databaseService.ReadAsync<Game>();
        var currentGame = games.First(e => e.TableId == tableId);

        switch (currentGame.CommunityCardsActions)
        {
            case CommunityCardsActions.PreFlop:
                var flop = currentGame.Deck.DrawCards(3);
                currentGame.TableCards.AddRange(flop);
                currentGame.CommunityCardsActions++;
                await _databaseService.ReplaceOneAsync(currentGame);
                await Clients.Group(tableId)
                    .SendAsync("ReceiveFlop", flop);
                break;

            case CommunityCardsActions.Flop:
                var turn = currentGame.Deck.DrawCards(1);
                currentGame.TableCards.AddRange(turn);
                currentGame.CommunityCardsActions++;
                await _databaseService.ReplaceOneAsync(currentGame);
                await Clients.Group(tableId)
                    .SendAsync("ReceiveTurnOrRiver", turn);
                break;

            case CommunityCardsActions.Turn:
                var river = currentGame.Deck.DrawCards(1);
                currentGame.TableCards.AddRange(river);
                currentGame.CommunityCardsActions++;
                await _databaseService.ReplaceOneAsync(currentGame);
                await Clients.Group(tableId)
                    .SendAsync("ReceiveTurnOrRiver", river);
                break;

            case CommunityCardsActions.River:
                await GetAndAwardWinners(tableId);
                await PlayerStateRefresh(tableId);
                currentGame.CommunityCardsActions++;
                await _databaseService.ReplaceOneAsync(currentGame);
                break;
        }
    }

    private async Task UpdatePot(string tableId)
    {
        var games = await _databaseService.ReadAsync<Game>();
        var currentGame = games.First(e => e.TableId == tableId);
        var players = currentGame
            .Players.Where(player => player is { RoundBet: > 0, ActionState: PlayerActionState.Playing })
            .ToList();

        while (players.Any())
        {
            var pot = new Pot { PotAmount = players.Min(e => e.RoundBet) };

            foreach (var player in players)
            {
                player.RoundBet -= pot.PotAmount;
                pot.Players.Add(player.Name);
            }

            pot.PotAmount *= players.Count;

            if (currentGame.Winnings
                    .Count(winningPot => winningPot.Players.SetEquals(pot.Players)) > 0)
            {
                currentGame.Winnings.First(e => e.Players.SetEquals(pot.Players)).PotAmount +=
                    pot.PotAmount;
            }
            else
            {
                currentGame.Winnings.Add(pot);
            }

            players = players.Where(e => e.RoundBet > 0).ToList();
            await _databaseService.ReplaceOneAsync(currentGame);

        }
    }

    private async Task GetAndAwardWinners(string tableId)
    {
        var users = await _databaseService.ReadAsync<User>();

        var games = await _databaseService.ReadAsync<Game>();
        var game = games.First(e => e.TableId == tableId);

        var communityCards = game.TableCards;
        var evaluatedPlayers = new Hashtable();

        foreach (var player in game.Players.Where(e => e.ActionState == PlayerActionState.Playing))
        {
            player.HandStrength = HandEvaluation.Evaluate(communityCards.Concat(player.HandCards).ToList());
            evaluatedPlayers.Add(player.Name, player.HandStrength);
        }

        foreach (var pot in game.Winnings)
        {
            var highestHand = HandStrength.Nothing;
            string winner = null;
            foreach (var potPlayer in pot.Players.Where(potPlayer => highestHand > (HandStrength)evaluatedPlayers[potPlayer]))
            {
                highestHand = (HandStrength)evaluatedPlayers[potPlayer];
                winner = potPlayer;
            }
            pot.Winner = winner;
            var user = users.First(e => e.Name == pot.Winner);
            user.Balance += pot.PotAmount;
            await _databaseService.ReplaceOneAsync(user);
        }

        await _databaseService.ReplaceOneAsync(game);
    }

    public async Task PlayerStateRefresh(string tableId)
    {
        try
        {
            var playerState = new PlayerStateModel();

            // Fetch necessary data from the database
            var users = await _databaseService.ReadAsync<User>(u => u.TableId == tableId);
            var game = (await _databaseService.ReadAsync<Game>(g => g.TableId == tableId)).FirstOrDefault();

            if (game != null)
            {
                // Populate playerState.Players based on users and game state
                foreach (var user in users)
                {
                    var gamePlayer = new GamePlayer
                    {
                        Username = user.Name,
                        IsPlaying = user.InGame,
                        IsReady = user.IsReady,
                        SeatNumber = user.SeatNumber,
                        GameMoney = user.Balance
                    };

                    var playerInGame = game.Players.FirstOrDefault(p => p.Name == user.Name);
                    if (playerInGame != null)
                    {
                        gamePlayer.ActionState = playerInGame.ActionState;
                    }

                    playerState.Players.Add(gamePlayer);
                }

                // Populate other game state properties in playerState
                playerState.CommunityCards = game.TableCards;
                playerState.GameInProgress = playerState.CommunityCards != null;
                playerState.Pots = game.Winnings?.ToList();
                playerState.SmallBlind = game.SmallBlind;
                playerState.RaiseAmount = game.RaiseAmount;

                // Send the playerState to clients
                var gamePlayers = game.Players.ToList();
                foreach (var user in users)
                {
                    var gamePlayer = gamePlayers.FirstOrDefault(p => p.Name == user.Name);
                    playerState.HandCards = gamePlayer?.HandCards;

                    await Clients.Client(user.ConnectionId).SendAsync("ReceiveStateRefresh", playerState);
                }
            }
            else
            {
                // No active game found for the specified tableId
                await Clients.Group(tableId).SendAsync("ReceiveStateRefresh", playerState);
            }
        }
        catch (Exception ex)
        {
            // Log the exception details
            Console.WriteLine($"An error occurred while refreshing player state: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }


    private async Task<string> GetTableByUser(string name)
    {
        var user = await _databaseService.GetOneByNameAsync<User>(name) ?? throw new NullReferenceException($"There is no user with this username: {name}");
        return user.TableId;
    }

    private async Task<bool> IsUserInGameHub(string name)
    {
        _ = await _databaseService.GetOneByNameAsync<User>(name);
        return true;
    }
}