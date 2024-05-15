using System.Collections;
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
    private readonly IDatabaseService _database;

    public GameHub(UserManager<ApplicationUser> userManager, IDatabaseService database)
    {
        _userManager = userManager;
        _database = database;
    }

    public async Task SendMessage(string message)
    {
        if (!await IsUserInGameHub(Context.User.Identity.Name)) return;

        var newMessage = new GetMessageResult { Sender = Context.User.Identity.Name, Message = message };
        var user = await _database.GetOneByNameAsync<User>(Context.User.Identity.Name);

        await Clients.Groups(user.TableId).SendAsync("ReceiveMessage", newMessage);
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var userName = Context.User.Identity.Name;
        await DisconnectPlayer(userName);
        await base.OnDisconnectedAsync(exception);
    }

    private async Task DisconnectPlayer(string userName)
    {
        try
        {
            var user = await _database.GetOneByNameAsync<User>(userName);
            var tableId = user.TableId;
            var smallBlindIndexTemp = 0;

            if (user.InGame)
            {
                var games = await _database.ReadAsync<Game>();
                var game = games.Find(x => x.TableId == tableId);
                foreach (var player in game.Players.Where(player => player.Name == user.Name))
                {
                    player.ActionState = PlayerActionState.Left;
                }

                if (game.Players.Count(e => e.ActionState != PlayerActionState.Left) < 2)
                {
                    await UpdatePot(game);
                    await GetAndAwardWinners(game);
                    await PlayerStateRefresh(tableId);
                    smallBlindIndexTemp = game.SmallBlindIndex;
                    await _database.ReplaceOneAsync(game);
                    await _database.DeleteOneAsync(game);
                    Thread.Sleep(10000);
                }
            }

            await Withdraw(user);
            await _database.DeleteOneAsync(user);
            var users = await _database.ReadAsync<User>();
            foreach (var e in users.Where(e => e.TableId == tableId))
            {
                e.InGame = false;
                await _database.ReplaceOneAsync(e);
            }
            users = await _database.ReadAsync<User>();
            if (users.Count(e => e.IsReady) >= 2 && users.All(e => e.TableId != tableId))
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
        var currentTable = await _database.GetOneFromIdAsync<PokerTable>(tableId);
        var users = await _database.ReadAsync<User>();
        if (users.Count(e => e.TableId == tableId) >= currentTable.MaxPlayers)
        {
            await Clients.Client(Context.ConnectionId).SendAsync("ReceiveKick");
            return;
        }
        if (users.Any(e => e.Name == Context.User.Identity.Name))
        {
            await Clients.Client(Context.ConnectionId).SendAsync("ReceiveKick");
            return;
        }
        await Groups.AddToGroupAsync(Context.ConnectionId, tableId);
        var newSeatNumber = await AssignTableToUser(tableId);
        var name = Context.User.Identity.Name;
        var newUser =
            new User
            {
                Name = name,
                TableId = tableId,
                ConnectionId = Context.ConnectionId,
                SeatNumber = newSeatNumber,
                InGame = false,
                Balance = 0
            };
        _database.InsertOne(newUser);
        await PlayerStateRefresh(tableId);
    }

    private async Task<int> AssignTableToUser(string tableId)
    {
        var users = await _database.ReadAsync<User>();
        var occupiedSeats = users.Where(e => e.TableId == tableId).Select(e => e.SeatNumber).OrderBy(e => e).ToList();
        for (var i = 0; i < occupiedSeats.Count; i++)
        {
            if (occupiedSeats[i] != i) return i;
        }
        return occupiedSeats.Count;
    }

    public async Task MarkReady(int depositAmount)
    {
        var userName = Context.User.Identity.Name;
        if (!await IsUserInGameHub(userName)) return;
        var user = await _database.GetOneByNameAsync<User>(userName);
        var tableId = user.TableId;
        await Deposit(depositAmount, user);
        user.IsReady = true;
        await _database.ReplaceOneAsync(user);

        await PlayerStateRefresh(tableId);

        var users = await _database.ReadAsync<User>();
        var games = await _database.ReadAsync<Game>();
        if (users.Where(e => e.TableId == tableId).Count(e => e.IsReady) >= 2 && games.All(e => e.TableId != tableId))
        {
            await StartGame(tableId, 0);
        }
    }

    private async Task Deposit(int depositAmount, User gameUser)
    {
        var user = await _userManager.FindByNameAsync(gameUser.Name);

        if ( user.Currency >= depositAmount)
        {
            user.Currency -= depositAmount;
            await _userManager.UpdateAsync(user);

            gameUser.Balance = depositAmount; 
        }
    }

    public async Task UnmarkReady()
    {
        var userName = Context.User.Identity.Name;
        if (!await IsUserInGameHub(userName)) return;
        var user = await _database.GetOneByNameAsync<User>(userName);

        await Withdraw(user);

        user.IsReady = false;
        await _database.ReplaceOneAsync(user);

        await PlayerStateRefresh(user.TableId);
    }

    private async Task Withdraw(User gameUser)
    {
        var user = await _userManager.FindByNameAsync(gameUser.Name);

        if (user != null)
        {
            user.Currency += gameUser.Balance;
            gameUser.Balance = 0;

            await _userManager.UpdateAsync(user);
        }
    }

    private async Task StartGame(string tableId, int smallBlindPosition)
    {
        //Initialize Game
        var currentTableInfo = await _database.GetOneFromIdAsync<PokerTable>(tableId);

        var newGame = new Game(tableId, smallBlindPosition, currentTableInfo.SmallBlind);

        //Adding players to table
        var users = await _database.ReadAsync<User>();
        foreach (var user in users.Where(user => user.IsReady && user.TableId == tableId))
        {
            newGame.Players.Add(new Player { Name = user.Name, RoundBet = 0 });
            user.InGame = true;
            await _database.ReplaceOneAsync(user);
        }

        newGame.NormalizeAllIndexes();

        //Small blind
        var smallBlindUser = await _database.GetOneByNameAsync<User>(newGame.GetPlayerNameByIndex(newGame.SmallBlindIndex));
        if (smallBlindUser.Balance >= newGame.SmallBlind)
        {
            smallBlindUser.Balance -= newGame.SmallBlind;
            newGame.Players.First(e => e.Name == smallBlindUser.Name).RoundBet += newGame.SmallBlind;
        }
        else
        {
            newGame.Players.First(e => e.Name == smallBlindUser.Name).RoundBet += smallBlindUser.Balance;
            smallBlindUser.Balance = 0;
        }
        await _database.ReplaceOneAsync(smallBlindUser);

        //Big blind
        var bigBlindUser = await _database.GetOneByNameAsync<User>(newGame.GetPlayerNameByIndex(newGame.BigBlindIndex));
        if (bigBlindUser.Balance >= newGame.SmallBlind * 2)
        {
            bigBlindUser.Balance -= newGame.SmallBlind * 2;
            newGame.Players.First(e => e.Name == bigBlindUser.Name).RoundBet += newGame.SmallBlind * 2;
        }
        else
        {
            newGame.Players.First(e => e.Name == bigBlindUser.Name).RoundBet += bigBlindUser.Balance;
            bigBlindUser.Balance = 0;
        }
        await _database.ReplaceOneAsync(bigBlindUser);

        users = await _database.ReadAsync<User>();
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

        _database.InsertOne(newGame);

        //Notify about the end of preparations
        await PlayerStateRefresh(tableId);

        await Clients.Group(tableId)
            .SendAsync("ReceiveTurnPlayer", newGame.GetPlayerNameByIndex(newGame.Index));
    }

    public async Task ActionFold()
    {
        var userName = Context.User.Identity.Name;

        if (!await IsUserInGameHub(userName)) return;

        var user = await _database.GetOneByNameAsync<User>(userName);

        var tableId = user.TableId;
        var games = await _database.ReadAsync<Game>();
        var currentGame = games.First(x => x.TableId == tableId);

        if (currentGame.Players.Any() && currentGame.GetPlayerNameByIndex(currentGame.Index) == userName)
        {
            //PlayerFolded
            currentGame.Players.First(e => e.Name == userName).ActionState = PlayerActionState.Folded;

            //Remove from pots
            foreach (var pot in currentGame.Winnings)
            {
                pot.Players.Remove(userName);
            }
            await _database.ReplaceOneAsync(currentGame);

            //CheckIfOnlyOneLeft
            games = await _database.ReadAsync<Game>();
            currentGame = games.First(x => x.TableId == tableId);
            if (currentGame.Players.Count(e => e.ActionState == PlayerActionState.Playing) == 1)
            {
                await UpdatePot(currentGame);
                await GetAndAwardWinners(currentGame);
                await PlayerStateRefresh(tableId);

                Thread.Sleep(10000);

                await _database.DeleteOneAsync(currentGame);

                var users = await _database.ReadAsync<User>();
                if (users.Where(e => e.TableId == tableId).Count(e => e.IsReady) >= 2 && !users.Any(e => e.TableId == tableId))
                {
                    await StartGame(tableId, currentGame.SmallBlindIndex + 1);
                }
                else
                {
                    foreach (var e in users.Where(e => e.TableId == tableId))
                    {
                        e.InGame = false;
                        await _database.ReplaceOneAsync(e);
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
        if (!await IsUserInGameHub(userName)) return;
        var user = await _database.GetOneByNameAsync<User>(userName);
        var tableId = user.TableId;
        var games = await _database.ReadAsync<Game>();
        var currentGame = games.First(x=> x.TableId == tableId);
        if (currentGame.Players.Any() && currentGame.GetPlayerNameByIndex(currentGame.Index) == userName)
        {
            await MoveIndex(tableId, currentGame);
        }
    }

    public async Task ActionRaise(int raiseAmount)
    {
        var userName = Context.User.Identity.Name;
        if (!await IsUserInGameHub(Context.User.Identity.Name)) return;
        var user = await _database.GetOneByNameAsync<User>(userName);

        var tableId = user.TableId;
        var games = await _database.ReadAsync<Game>();
        var currentGame = games.First(e => e.TableId == tableId);
        var currentPlayer = currentGame.Players.First(e => e.Name == userName);

        if (currentGame.Players.Any() && currentGame.GetPlayerNameByIndex(currentGame.Index) == userName &&
            user.Balance > raiseAmount + currentGame.RaiseAmount - currentPlayer.RoundBet)
        {
            user.Balance -= raiseAmount + currentGame.RaiseAmount - currentPlayer.RoundBet;
            await _database.ReplaceOneAsync(user);

            currentPlayer.RoundBet = raiseAmount + currentGame.RaiseAmount;
            currentGame.RaiseAmount += raiseAmount;
            currentGame.RoundEndIndex = currentGame.Index;
            await _database.ReplaceOneAsync(currentGame);

            await MoveIndex(tableId, currentGame);
        }
    }

    public async Task ActionCall()
    {
        var userName = Context.User.Identity.Name;

        if (!await IsUserInGameHub(Context.User.Identity.Name)) return;

        var user = await _database.GetOneByNameAsync<User>(userName);
        var tableId = user.TableId;

        var games = await _database.ReadAsync<Game>();
        var currentGame = games.First(e => e.TableId == tableId);
        var currentPlayer = currentGame.Players.First(e => e.Name == userName);

        if (currentGame.Players.Any() && currentGame.GetPlayerNameByIndex(currentGame.Index) == userName && currentGame.RaiseAmount > 0)
        {
            if (currentGame.RaiseAmount - currentPlayer.RoundBet < user.Balance)
            {
                user.Balance -= currentGame.RaiseAmount - currentPlayer.RoundBet;
                currentPlayer.RoundBet = currentGame.RaiseAmount;
            }
            else if (currentGame.RaiseAmount - currentPlayer.RoundBet >= user.Balance)
            {
                var allInSum = user.Balance;
                user.Balance = 0;
                currentPlayer.RoundBet = allInSum;
            }
            await _database.ReplaceOneAsync(user);
            await _database.ReplaceOneAsync(currentGame);

            await MoveIndex(tableId, currentGame);
        }
    }

    public async Task ActionAllIn()
    {
        var userName = Context.User.Identity.Name;

        if (!await IsUserInGameHub(userName)) return;

        var user = await _database.GetOneByNameAsync<User>(userName);
        var tableId = user.TableId;

        var games = await _database.ReadAsync<Game>();
        var currentGame = games.First(e => e.TableId == tableId);
        var currentPlayer = currentGame.Players.First(e => e.Name == userName);

        if (currentGame.Players.Any() && currentGame.GetPlayerNameByIndex(currentGame.Index) == userName &&
            user.Balance > currentGame.RaiseAmount - currentPlayer.RoundBet)
        {
            var allInSum = user.Balance;
            user.Balance = 0;
            currentPlayer.RoundBet = allInSum;
            currentGame.RaiseAmount += allInSum;
            currentGame.RoundEndIndex = currentGame.Index;

            await _database.ReplaceOneAsync(user);
            await _database.ReplaceOneAsync(currentGame);

            await MoveIndex(tableId, currentGame);
        }
    }

    private async Task MoveIndex(string tableId, Game currentGame)
    {
        do
        {
            currentGame.SetIndex(currentGame.Index + 1);
            await _database.ReplaceOneAsync(currentGame);

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
                await _database.ReplaceOneAsync(currentGame);
            }
        } while ((currentGame.GetPlayerByIndex(currentGame.Index).ActionState != PlayerActionState.Playing || (await _database.GetOneByNameAsync<User>(currentGame.GetPlayerByIndex(currentGame.Index).Name)).Balance == 0
                     || currentGame.Players.Count(e => e.ActionState == PlayerActionState.Playing) < 2) && currentGame.CommunityCardsActions !=
                 CommunityCardsActions.AfterRiver);

        if (currentGame.CommunityCardsActions == CommunityCardsActions.AfterRiver)
        {
            Thread.Sleep(10000);

            await _database.DeleteOneAsync(currentGame);

            var users = await _database.ReadAsync<User>();
            var games = await _database.ReadAsync<Game>();
            if (users.Where(e => e.TableId == tableId).Count(e => e.IsReady) >= 2 && games.All(e => e.TableId != tableId))
            {
                await StartGame(tableId, currentGame.SmallBlindIndex + 1);
            }
            else
            {
                foreach (var e in users.Where(e => e.TableId == tableId))
                {
                    e.InGame = false;
                    await _database.ReplaceOneAsync(e);
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

    public async Task CommunityCardsController(Game currentGame)
    {
        var games = await _database.ReadAsync<Game>();

        switch (currentGame.CommunityCardsActions)
        {
            case CommunityCardsActions.PreFlop:
                var flop = currentGame.Deck.DrawCards(3);
                currentGame.TableCards.AddRange(flop);
                currentGame.CommunityCardsActions++;
                await _database.ReplaceOneAsync(currentGame);
                await Clients.Group(currentGame.TableId)
                    .SendAsync("ReceiveFlop", flop);
                break;

            case CommunityCardsActions.Flop:
                var turn = currentGame.Deck.DrawCards(1);
                currentGame.TableCards.AddRange(turn);
                currentGame.CommunityCardsActions++;
                await _database.ReplaceOneAsync(currentGame);
                await Clients.Group(currentGame.TableId)
                    .SendAsync("ReceiveTurnOrRiver", turn);
                break;

            case CommunityCardsActions.Turn:
                var river = currentGame.Deck.DrawCards(1);
                currentGame.TableCards.AddRange(river);
                currentGame.CommunityCardsActions++;
                await _database.ReplaceOneAsync(currentGame);
                await Clients.Group(currentGame.TableId)
                    .SendAsync("ReceiveTurnOrRiver", river);
                break;

            case CommunityCardsActions.River:
                await GetAndAwardWinners(currentGame);
                await PlayerStateRefresh(currentGame.TableId);
                currentGame.CommunityCardsActions++;
                await _database.ReplaceOneAsync(currentGame);
                break;
        }
    }

    private async Task UpdatePot(Game currentGame)
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
            await _database.ReplaceOneAsync(currentGame);

            players = players.Where(e => e.RoundBet > 0).ToList();

        }
    }

    private async Task GetAndAwardWinners(Game currentGame)
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
            foreach (var potPlayer in pot.Players.Where(potPlayer => highestHand > (HandStrength)evaluatedPlayers[potPlayer]))
            {
                highestHand = (HandStrength)evaluatedPlayers[potPlayer];
                winner = potPlayer;
            }
            pot.Winner = winner;
            var winningUser = await _database.GetOneByNameAsync<User>(pot.Winner);
            winningUser.Balance += pot.PotAmount;
            await _database.ReplaceOneAsync(winningUser);
        }
    }


    public async Task PlayerStateRefresh(string tableId)
    {
        var playerState = new PlayerStateModel();
        var users = await _database.ReadAsync<User>();
        var games = await _database.ReadAsync<Game>();

        foreach (var user in users.Where(e => e.TableId == tableId))
        {
            playerState.Players.Add(new GamePlayer
            {
                Username = user.Name,
                IsPlaying = user.InGame,
                IsReady = user.IsReady,
                SeatNumber = user.SeatNumber,
                GameMoney = user.Balance
            });

            if (games.FirstOrDefault(e => e.TableId == tableId) != null &&
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

        var gamePlayers = games.FirstOrDefault(e => e.TableId == tableId)?.Players;

        if (gamePlayers == null)
        {
            await Clients.Group(tableId).SendAsync("ReceiveStateRefresh", playerState);
        }
        else
        {
            foreach (var user in users.Where(e => e.TableId == tableId))
            {
                playerState.HandCards = gamePlayers.FirstOrDefault(e => e.Name == user.Name)?.HandCards;
                await Clients.Client(user.ConnectionId).SendAsync("ReceiveStateRefresh", playerState);
            }
        }
    }
    private async Task<bool> IsUserInGameHub(string name)
    {
        var users = await _database.ReadAsync<User>();
        return users.Select(e => e.Name).Contains(name);
    }
}