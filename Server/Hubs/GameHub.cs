using System.Collections;
using JokersJunction.Server.Evaluators;
using JokersJunction.Server.Models;
using JokersJunction.Server.Repositories.Contracts;
using JokersJunction.Shared;
using JokersJunction.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace JokersJunction.Server.Hubs;

public class GameHub : Hub
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITableRepository _tableRepository;

    public static List<PokerGame> PokerGames { get; set; } = new();
    private static List<BlackjackGame> BlackjackGames { get; set; } = new();

    public static List<User> Users { get; set; } = new();

    public GameHub(UserManager<ApplicationUser> userManager, ITableRepository tableRepository)
    {
        _userManager = userManager;
        _tableRepository = tableRepository;
    }

    public async Task SendMessage(string message)
    {
        if (!IsUserInGameHub(Context.User.Identity.Name)) return;

        var newMessage = new GetMessageResult { Sender = Context.User.Identity.Name, Message = message };


        await Clients.Groups(GetTableByUser(Context.User.Identity.Name).ToString()).SendAsync("ReceiveMessage", newMessage);
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        await DisconnectPlayer(Context.User.Identity.Name);
        await base.OnDisconnectedAsync(exception);
    }

    private async Task DisconnectPlayer(string user)
    {
        try
        {
            var tableId = Users.FirstOrDefault(e => e.Name == user)?.TableId ?? -1;
            var smallBlindIndexTemp = 0;

            if (Users.First(e => e.Name == user).InGame)
            {
                foreach (var player in PokerGames.SelectMany(game => game.Players.Where(player => player.Name == user)))
                {
                    player.ActionState = PlayerActionState.Left;
                }

                if (PokerGames.First(e => e.TableId == tableId).Players.Count(e => e.ActionState != PlayerActionState.Left) < 2)
                {
                    UpdatePot(tableId);
                    GetAndAwardWinners(tableId);
                    PokerPlayerStateRefresh(tableId);
                    smallBlindIndexTemp = PokerGames.First(e => e.TableId == tableId).SmallBlindIndex;
                    PokerGames.Remove(PokerGames.FirstOrDefault(e => e.TableId == tableId));
                    Thread.Sleep(10000);
                }
            }

            await Withdraw(user);
            Users.Remove(Users.FirstOrDefault(e => e.Name == user));
            foreach (var e in Users.Where(e => e.TableId == tableId))
            {
                e.InGame = false;
            }
            if (Users.Count(e => e.IsReady) >= 2 && PokerGames.All(e => e.TableId != tableId))
            {
                await StartPokerGame(tableId, smallBlindIndexTemp + 1);
            }
            else
            {
                PokerPlayerStateRefresh(tableId);
            }
        }
        catch (Exception)
        {
            //Log
        }
    }


    public async Task AddToUsersToPokerTable(int tableId)
    {
        var currentTable = await _tableRepository.GetPokerTableById(tableId);

        if (Users.Count(e => e.TableId == tableId) >= currentTable.MaxPlayers)
        {
            await Clients.Client(Context.ConnectionId).SendAsync("ReceiveKick");
            return;
        }

        if (Users.Any(e => e.Name == Context.User.Identity.Name))
        {
            await Clients.Client(Context.ConnectionId).SendAsync("ReceiveKick");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, tableId.ToString());

        var newSeatNumber = AssignTableToUser(tableId);
        var name = Context.User.Identity.Name;

        Users.Add(new User
        {
            Name = name,
            TableId = tableId,
            ConnectionId = Context.ConnectionId,
            SeatNumber = newSeatNumber,
            InGame = false,
            Balance = 0
        });

        PokerPlayerStateRefresh(tableId);
    }
    public async Task AddToUsersToBlackjack(int tableId)
    {
        var currentTable = await _tableRepository.GetBlackjackTableById(tableId);

        if (Users.Count(e => e.TableId == tableId) >= currentTable.MaxPlayers)
        {
            await Clients.Client(Context.ConnectionId).SendAsync("ReceiveKick");
            return;
        }

        if (Users.Any(e => e.Name == Context.User.Identity.Name))
        {
            await Clients.Client(Context.ConnectionId).SendAsync("ReceiveKick");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, tableId.ToString());

        var newSeatNumber = AssignTableToUser(tableId);
        var name = Context.User.Identity.Name;

        Users.Add(new User
        {
            Name = name,
            TableId = tableId,
            ConnectionId = Context.ConnectionId,
            SeatNumber = newSeatNumber,
            InGame = false,
            Balance = 0
        });

        BlackjackPlayerStateRefresh(tableId);
    }

    private static int AssignTableToUser(int tableId)
    {
        var occupiedSeats = Users.Where(e => e.TableId == tableId).Select(e => e.SeatNumber).OrderBy(e => e).ToList();
        for (var i = 0; i < occupiedSeats.Count; i++)
        {
            if (occupiedSeats[i] != i)
                return i;
        }
        return occupiedSeats.Count;

    }

    public async Task MarkReady(int depositAmount)
    {
        if (!IsUserInGameHub(Context.User.Identity.Name)) return;
        var tableId = GetTableByUser(Context.User.Identity.Name);

        var user = Context.User.Identity.Name;
        await Deposit(depositAmount, user);

        Users.First(e => e.Name == user).IsReady = true;
        PokerPlayerStateRefresh(tableId);

        if (Users.Where(e => e.TableId == tableId).Count(e => e.IsReady) >= 2 && PokerGames.All(e => e.TableId != tableId))
        {
            await StartPokerGame(tableId, 0);
        }
    }

    private async Task Deposit(int depositAmount, string userName)
    {
        var user = await _userManager.FindByNameAsync(userName);

        if (user != null && user.Currency >= depositAmount)
        {
            user.Currency -= depositAmount;
            await _userManager.UpdateAsync(user);
            Users.First(e => e.Name == user.UserName).Balance = depositAmount;
        }
    }

    public async Task UnmarkReady()
    {
        if (!IsUserInGameHub(Context.User.Identity.Name)) return;

        var user = Context.User.Identity.Name;
        await Withdraw(user);
        Users.First(e => e.Name == user).IsReady = false;

        PokerPlayerStateRefresh(GetTableByUser(Context.User.Identity.Name));
    }

    private async Task Withdraw(string userName)
    {
        var user = await _userManager.FindByNameAsync(userName);
        user.Currency += Users.First(e => e.Name == user.UserName).Balance;
        Users.First(e => e.Name == user.UserName).Balance = 0;
        await _userManager.UpdateAsync(user);
    }

    public async Task StartBlackjackGame(int tableId)
    {
        var initialBet = 10; // Define initial bet amount
        var game = new BlackjackGame(tableId, initialBet);
        BlackjackGames.Add(game);

        // Initialize player balances and bets similar to Poker
        foreach (var user in Users.Where(user => user.TableId == tableId && user.IsReady))
        {
            var player = new BlackjackPlayer { Name = user.Name, RoundBet = 0 };
            player.HandCards.AddRange(game.Deck.DrawCards(2));
            game.Players.Add(player);

            if (user.Balance > 0)
            {
                // Deduct initial bet - similar to posting blinds in Poker
                if (user.Balance >= initialBet)
                {
                    user.Balance -= initialBet;
                    player.RoundBet = initialBet;
                }
                else
                {
                    player.RoundBet = user.Balance;
                    user.Balance = 0;
                }
            }

            await Clients.Client(user.ConnectionId).SendAsync("ReceiveBlackjackStartingHand", player.HandCards);
        }

        await Clients.Group(tableId.ToString()).SendAsync("ReceiveBlackjackDealerHand", game.DealerHand.First());
    }

    public async Task BlackjackHit(string playerName)
    {
        var game = BlackjackGames.FirstOrDefault(g => g.Players.Any(p => p.Name == playerName));
        var player = game?.Players.FirstOrDefault(p => p.Name == playerName);

        if (game != null && player != null && !player.IsBust && !player.IsStand)
        {
            var card = game.Deck.DrawCards(1);
            player.HandCards.Add(card.First());

            if (player.GetHandValue() > 21)
            {
                player.IsBust = true;
                await Clients.Client(Context.ConnectionId).SendAsync("ReceiveBlackjackBust", player.HandCards);
            }
            else
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ReceiveBlackjackHit", player.HandCards);
            }
        }
    }

    public async Task BlackjackStand(string playerName)
    {
        var game = BlackjackGames.FirstOrDefault(g => g.Players.Any(p => p.Name == playerName));
        var player = game?.Players.FirstOrDefault(p => p.Name == playerName);

        if (game != null && player != null)
        {
            player.IsStand = true;
            await Clients.Client(Context.ConnectionId).SendAsync("ReceiveBlackjackStand");

            if (game.Players.All(p => p.IsStand || p.IsBust))
            {
                await CompleteBlackjackGame(game.TableId);
            }
        }
    }

    public async Task BlackjackBet(string playerName, int betAmount)
    {
        var user = Users.FirstOrDefault(u => u.Name == playerName);
        var game = BlackjackGames.FirstOrDefault(g => g.Players.Any(p => p.Name == playerName));
        var player = game?.Players.FirstOrDefault(p => p.Name == playerName);

        if (user != null && game != null && player != null && user.Balance >= betAmount)
        {
            user.Balance -= betAmount;
            player.RoundBet += betAmount;

            await Clients.Client(user.ConnectionId).SendAsync("ReceiveBlackjackBet", player.RoundBet);

            // Check if all players have placed their bets, then proceed
            if (game.Players.All(p => p.RoundBet > 0))
            {
                await Clients.Group(game.TableId.ToString()).SendAsync("ReceiveBlackjackDealerHand", game.DealerHand);
            }
        }
    }

    private async Task CompleteBlackjackGame(int tableId)
    {
        var game = BlackjackGames.FirstOrDefault(g => g.TableId == tableId);

        if (game != null)
        {
            while (game.GetDealerHandValue() < 17)
            {
                game.DealerHand.Add(game.Deck.DrawCards(1).First());
            }

            foreach (var player in game.Players)
            {
                if (player.IsBust || (game.GetDealerHandValue() <= 21 && game.GetDealerHandValue() > player.GetHandValue()))
                {
                    await Clients.Client(Users.First(u => u.Name == player.Name).ConnectionId).SendAsync("ReceiveBlackjackLose", game.DealerHand);
                }
                else if (game.GetDealerHandValue() > 21 || player.GetHandValue() > game.GetDealerHandValue())
                {
                    var winAmount = player.RoundBet * 2;
                    Users.First(u => u.Name == player.Name).Balance += winAmount;
                    await Clients.Client(Users.First(u => u.Name == player.Name).ConnectionId).SendAsync("ReceiveBlackjackWin", game.DealerHand);
                }
                else
                {
                    var pushAmount = player.RoundBet;
                    Users.First(u => u.Name == player.Name).Balance += pushAmount;
                    await Clients.Client(Users.First(u => u.Name == player.Name).ConnectionId).SendAsync("ReceiveBlackjackDraw", game.DealerHand);
                }
            }

            BlackjackGames.Remove(game);
        }
    }

    private async Task StartPokerGame(int tableId, int smallBlindPosition)
    {
        //Initialize Game
        var currentTableInfo = await _tableRepository.GetPokerTableById(tableId);

        var newGame = new PokerGame(tableId, smallBlindPosition, currentTableInfo.SmallBlind);

        //Adding players to table
        foreach (var user in Users.Where(user => user.IsReady && user.TableId == tableId))
        {
            newGame.Players.Add(new PokerPlayer() { Name = user.Name, RoundBet = 0 });
            user.InGame = true;
        }

        newGame.NormalizeAllIndexes();

        //Small blind
        if (Users.First(e => e.Name == newGame.GetPlayerNameByIndex(newGame.SmallBlindIndex)).Balance >=
            newGame.SmallBlind)
        {
            Users.First(e => e.Name == newGame.GetPlayerNameByIndex(newGame.SmallBlindIndex)).Balance -=
                newGame.SmallBlind;
            newGame.Players.First(e => e.Name == newGame.GetPlayerNameByIndex(newGame.SmallBlindIndex)).RoundBet +=
                newGame.SmallBlind;
        }
        else
        {
            newGame.Players.First(e => e.Name == newGame.GetPlayerNameByIndex(newGame.SmallBlindIndex)).RoundBet +=
                Users.First(e => e.Name == newGame.GetPlayerNameByIndex(newGame.SmallBlindIndex)).Balance;
            Users.First(e => e.Name == newGame.GetPlayerNameByIndex(newGame.SmallBlindIndex)).Balance = 0;
        }

        //Big blind
        if (Users.First(e => e.Name == newGame.GetPlayerNameByIndex(newGame.BigBlindIndex)).Balance >=
newGame.SmallBlind * 2)
        {
            Users.First(e => e.Name == newGame.GetPlayerNameByIndex(newGame.BigBlindIndex)).Balance -=
                newGame.SmallBlind * 2;
            newGame.Players.First(e => e.Name == newGame.GetPlayerNameByIndex(newGame.BigBlindIndex)).RoundBet +=
                newGame.SmallBlind * 2;
        }
        else
        {
            newGame.Players.First(e => e.Name == newGame.GetPlayerNameByIndex(newGame.BigBlindIndex)).RoundBet +=
                Users.First(e => e.Name == newGame.GetPlayerNameByIndex(newGame.BigBlindIndex)).Balance;
            Users.First(e => e.Name == newGame.GetPlayerNameByIndex(newGame.BigBlindIndex)).Balance = 0;
        }

        //Deal cards

        foreach (var player in newGame.Players)
        {
            if (Users.Where(e => e.TableId == tableId && e.InGame).Select(e => e.Name).ToList().Contains(player.Name))
            {
                player.HandCards.AddRange(newGame.Deck.DrawCards(2));
                var connectionId = Users.First(e => e.Name == player.Name).ConnectionId;
                if (Users.First(e => e.Name == player.Name).InGame)
                    await Clients.Client(connectionId).SendAsync("ReceiveStartingHand", player.HandCards);
            }
        }

        PokerGames.Add(newGame);

        //Notify about the end of preparations

        PokerPlayerStateRefresh(tableId);

        await Clients.Group(tableId.ToString())
            .SendAsync("ReceiveTurnPlayer", newGame.GetPlayerNameByIndex(newGame.Index));
    }

    public async Task ActionFold()
    {
        if (!IsUserInGameHub(Context.User.Identity.Name)) return;
        var tableId = GetTableByUser(Context.User.Identity.Name);

        var currentGame = PokerGames.First(e => e.TableId == tableId);

        if (currentGame.Players.Any() &&
            currentGame.GetPlayerNameByIndex(currentGame.Index) == Context.User.Identity.Name)
        {
            //PlayerFolded
            PokerGames.First(e => e.TableId == tableId).Players
                .First(e => e.Name == Context.User.Identity.Name).ActionState = PlayerActionState.Folded;

            //Remove from pots
            foreach (var pot in PokerGames.First(e => e.TableId == tableId).Winnings)
            {
                pot.Players.Remove(Context.User.Identity.Name);
            }

            //CheckIfOnlyOneLeft
            if (PokerGames.First(e => e.TableId == tableId).Players
                    .Count(e => e.ActionState == PlayerActionState.Playing) == 1)
            {
                UpdatePot(tableId);
                GetAndAwardWinners(tableId);
                PokerPlayerStateRefresh(tableId);

                Thread.Sleep(10000);

                PokerGames.Remove(PokerGames.FirstOrDefault(e => e.TableId == tableId));

                if (Users.Where(e => e.TableId == tableId).Count(e => e.IsReady) >= 2 && PokerGames.All(e => e.TableId != tableId))
                {
                    await StartPokerGame(tableId, currentGame.SmallBlindIndex + 1);
                }
                else
                {
                    foreach (var e in Users.Where(e => e.TableId == tableId))
                    {
                        e.InGame = false;
                    }
                    PokerPlayerStateRefresh(tableId);
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
        if (!IsUserInGameHub(Context.User.Identity.Name)) return;
        var tableId = GetTableByUser(Context.User.Identity.Name);

        var currentGame = PokerGames.First(e => e.TableId == tableId);

        if (currentGame.Players.Any() &&
            currentGame.GetPlayerNameByIndex(currentGame.Index) == Context.User.Identity.Name)
        {

            await MoveIndex(tableId, currentGame);
        }
    }

    public async Task ActionRaise(int raiseAmount)
    {
        if (!IsUserInGameHub(Context.User.Identity.Name)) return;
        var tableId = GetTableByUser(Context.User.Identity.Name);

        var currentGame = PokerGames.First(e => e.TableId == tableId);
        var currentPlayer = currentGame.Players.First(e => e.Name == Context.User.Identity.Name);

        if (currentGame.Players.Any() &&
            currentGame.GetPlayerNameByIndex(currentGame.Index) == Context.User.Identity.Name &&
            Users.First(e => e.Name == Context.User.Identity.Name).Balance > raiseAmount + currentGame.RaiseAmount - currentPlayer.RoundBet)
        {
            Users.First(e => e.Name == Context.User.Identity.Name).Balance -= raiseAmount + currentGame.RaiseAmount - currentPlayer.RoundBet;

            PokerGames.First(e => e.TableId == tableId).Players.First(e => e.Name == Context.User.Identity.Name).RoundBet = raiseAmount + currentGame.RaiseAmount;

            PokerGames.First(e => e.TableId == tableId).RaiseAmount += raiseAmount;

            PokerGames.First(e => e.TableId == tableId).RoundEndIndex =
                PokerGames.First(e => e.TableId == tableId).Index;

            await MoveIndex(tableId, currentGame);
        }
    }

    public async Task ActionCall()
    {
        if (!IsUserInGameHub(Context.User.Identity.Name)) return;
        var tableId = GetTableByUser(Context.User.Identity.Name);

        var currentGame = PokerGames.First(e => e.TableId == tableId);
        var currentPlayer = currentGame.Players.First(e => e.Name == Context.User.Identity.Name);

        if (currentGame.Players.Any() &&
            currentGame.GetPlayerNameByIndex(currentGame.Index) == Context.User.Identity.Name &&
            currentGame.RaiseAmount > 0)
        {
            if (currentGame.RaiseAmount - currentPlayer.RoundBet <
                Users.First(e => e.TableId == tableId && e.Name == Context.User.Identity.Name).Balance)
            {
                Users.First(e => e.Name == Context.User.Identity.Name).Balance -= currentGame.RaiseAmount - currentPlayer.RoundBet;
                PokerGames.First(e => e.TableId == tableId).Players.First(e => e.Name == Context.User.Identity.Name).RoundBet = currentGame.RaiseAmount;
            }
            else if (currentGame.RaiseAmount - currentPlayer.RoundBet >=
                     Users.First(e => e.TableId == tableId && e.Name == Context.User.Identity.Name).Balance)
            {
                var allInSum = Users.First(e => e.Name == Context.User.Identity.Name).Balance;
                Users.First(e => e.Name == Context.User.Identity.Name).Balance = 0;
                PokerGames.First(e => e.TableId == tableId).Players.First(e => e.Name == Context.User.Identity.Name).RoundBet = allInSum;
            }
            await MoveIndex(tableId, currentGame);
        }
    }

    public async Task ActionAllIn()
    {
        if (!IsUserInGameHub(Context.User.Identity.Name)) return;
        var tableId = GetTableByUser(Context.User.Identity.Name);

        var currentGame = PokerGames.First(e => e.TableId == tableId);
        var currentPlayer = currentGame.Players.First(e => e.Name == Context.User.Identity.Name);

        if (currentGame.Players.Any() &&
            currentGame.GetPlayerNameByIndex(currentGame.Index) == Context.User.Identity.Name &&
            Users.First(e => e.Name == Context.User.Identity.Name).Balance > currentGame.RaiseAmount - currentPlayer.RoundBet)
        {
            var allInSum = Users.First(e => e.Name == Context.User.Identity.Name).Balance;
            Users.First(e => e.Name == Context.User.Identity.Name).Balance = 0;
            PokerGames.First(e => e.TableId == tableId).Players.First(e => e.Name == Context.User.Identity.Name).RoundBet = allInSum;
            PokerGames.First(e => e.TableId == tableId).RaiseAmount += allInSum;

            PokerGames.First(e => e.TableId == tableId).RoundEndIndex =
                PokerGames.First(e => e.TableId == tableId).Index;

            await MoveIndex(tableId, currentGame);
        }
    }

    private async Task MoveIndex(int tableId, PokerGame currentGame)
    {
        do
        {
            currentGame.SetIndex(currentGame.Index + 1);
            PokerGames.FirstOrDefault(e => e.TableId == tableId)?.SetIndex(currentGame.Index);

            if (PokerGames.First(e => e.TableId == tableId).Index == currentGame.RoundEndIndex)
            {
                CommunityCardsController(tableId);
                UpdatePot(tableId);
                PokerGames.First(e => e.TableId == tableId).RaiseAmount = 0;
                PokerGames.First(e => e.TableId == tableId).RoundEndIndex = PokerGames.First(e => e.TableId == tableId).BigBlindIndex + 1;
                PokerGames.First(e => e.TableId == tableId).Index = PokerGames.First(e => e.TableId == tableId).BigBlindIndex + 1;
                PokerGames.First(e => e.TableId == tableId).NormalizeAllIndexes();
                foreach (var player in PokerGames.First(e => e.TableId == tableId).Players)
                {
                    player.RoundBet = 0;
                }
            }
        } while ((currentGame.GetPlayerByIndex(currentGame.Index).ActionState != PlayerActionState.Playing || Users.First(e => e.Name == currentGame.GetPlayerByIndex(currentGame.Index).Name).Balance == 0
                             || currentGame.Players.Count(e => e.ActionState == PlayerActionState.Playing) < 2) && PokerGames.First(e => e.TableId == tableId).CommunityCardsActions !=
                             CommunityCardsActions.AfterRiver);

        if (PokerGames.First(e => e.TableId == tableId).CommunityCardsActions ==
            CommunityCardsActions.AfterRiver)
        {
            Thread.Sleep(10000);

            PokerGames.Remove(PokerGames.FirstOrDefault(e => e.TableId == tableId));

            if (Users.Where(e => e.TableId == tableId).Count(e => e.IsReady) >= 2 && PokerGames.All(e => e.TableId != tableId))
            {
                await StartPokerGame(tableId, currentGame.SmallBlindIndex + 1);
            }
            else
            {
                foreach (var e in Users.Where(e => e.TableId == tableId))
                {
                    e.InGame = false;
                }
                PokerPlayerStateRefresh(tableId);
            }
        }
        else
        {
            PokerPlayerStateRefresh(tableId);
            await Clients.Group(tableId.ToString())
                .SendAsync("ReceiveTurnPlayer",
                    currentGame.GetPlayerNameByIndex(PokerGames.First(e => e.TableId == tableId).Index));
        }
    }

    public void CommunityCardsController(int tableId)
    {
        var currentGame = PokerGames.First(e => e.TableId == tableId);

        switch (currentGame.CommunityCardsActions)
        {
            case CommunityCardsActions.PreFlop:
                var flop = PokerGames.FirstOrDefault(e => e.TableId == tableId)?.Deck.DrawCards(3);
                PokerGames.FirstOrDefault(e => e.TableId == tableId)?.TableCards.AddRange(flop);
                PokerGames.First(e => e.TableId == tableId).CommunityCardsActions++;
                Clients.Group(tableId.ToString())
                    .SendAsync("ReceiveFlop", flop);

                break;

            case CommunityCardsActions.Flop:
                var turn = PokerGames.FirstOrDefault(e => e.TableId == tableId)?.Deck.DrawCards(1);
                PokerGames.FirstOrDefault(e => e.TableId == tableId)?.TableCards.AddRange(turn);
                PokerGames.First(e => e.TableId == tableId).CommunityCardsActions++;
                Clients.Group(tableId.ToString())
                    .SendAsync("ReceiveTurnOrRiver", turn);
                break;

            case CommunityCardsActions.Turn:
                var river = PokerGames.FirstOrDefault(e => e.TableId == tableId)?.Deck.DrawCards(1);
                PokerGames.FirstOrDefault(e => e.TableId == tableId)?.TableCards.AddRange(river);
                PokerGames.First(e => e.TableId == tableId).CommunityCardsActions++;
                Clients.Group(tableId.ToString())
                    .SendAsync("ReceiveTurnOrRiver", river);
                break;

            case CommunityCardsActions.River:
                GetAndAwardWinners(tableId);
                PokerPlayerStateRefresh(tableId);
                PokerGames.First(e => e.TableId == tableId).CommunityCardsActions++;
                break;
        }
    }

    private static void UpdatePot(int tableId)
    {
        var players = PokerGames.First(e => e.TableId == tableId)
            .Players.Where(player => player.RoundBet > 0 && player.ActionState == PlayerActionState.Playing)
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

            if (PokerGames.First(e => e.TableId == tableId).Winnings
                    .Count(winningPot => winningPot.Players.SetEquals(pot.Players)) > 0)
            {
                PokerGames.First(e => e.TableId == tableId).Winnings.First(e => e.Players.SetEquals(pot.Players)).PotAmount +=
                    pot.PotAmount;
            }
            else
            {
                PokerGames.First(e => e.TableId == tableId).Winnings.Add(pot);
            }

            players = players.Where(e => e.RoundBet > 0).ToList();
        }
    }

    private void GetAndAwardWinners(int tableId)
    {
        var communityCards = PokerGames.First(e => e.TableId == tableId).TableCards;
        var evaluatedPlayers = new Hashtable();

        foreach (var player in PokerGames.First(e => e.TableId == tableId).Players.Where(e => e.ActionState == PlayerActionState.Playing))
        {
            player.HandStrength = HandEvaluation.Evaluate(communityCards.Concat(player.HandCards).ToList());
            evaluatedPlayers.Add(player.Name, player.HandStrength);
        }

        foreach (var pot in PokerGames.First(e => e.TableId == tableId).Winnings)
        {
            var highestHand = HandStrength.Nothing;
            string winner = null;
            foreach (var potPlayer in pot.Players.Where(potPlayer => highestHand > (HandStrength)evaluatedPlayers[potPlayer]))
            {
                highestHand = (HandStrength)evaluatedPlayers[potPlayer];
                winner = potPlayer;
            }
            pot.Winner = winner;
            Users.First(e => e.Name == pot.Winner).Balance += pot.PotAmount;
        }
    }

    public void PokerPlayerStateRefresh(int tableId)
    {
        var playerState = new PlayerStateModel();

        foreach (var user in Users.Where(e => e.TableId == tableId))
        {
            playerState.Players.Add(new GamePlayer
            {
                Username = user.Name,
                IsPlaying = user.InGame,
                IsReady = user.IsReady,
                SeatNumber = user.SeatNumber,
                GameMoney = user.Balance
            });

            if (PokerGames.FirstOrDefault(e => e.TableId == tableId) != null &&
                PokerGames.First(e => e.TableId == tableId).Players.Select(e => e.Name).Contains(user.Name))
            {
                playerState.Players.Last().ActionState = PokerGames.First(e => e.TableId == tableId).Players
                    .First(e => e.Name == user.Name).ActionState;
            }
        }

        playerState.CommunityCards = PokerGames.FirstOrDefault(e => e.TableId == tableId)?.TableCards;

        playerState.GameInProgress = playerState.CommunityCards != null;

        playerState.Pots = PokerGames.FirstOrDefault(e => e.TableId == tableId)?.Winnings;

        if (PokerGames.FirstOrDefault(e => e.TableId == tableId) != null)
            playerState.SmallBlind = PokerGames.First(e => e.TableId == tableId).SmallBlind;

        if (PokerGames.FirstOrDefault(e => e.TableId == tableId)?.RaiseAmount > 0)
            playerState.RaiseAmount = PokerGames.First(e => e.TableId == tableId).RaiseAmount;

        var gamePlayers = PokerGames.FirstOrDefault(e => e.TableId == tableId)?.Players;

        if (gamePlayers == null)
        {
            Clients.Group(tableId.ToString()).SendAsync("ReceiveStateRefresh", playerState);
        }
        else
        {
            foreach (var user in Users.Where(e => e.TableId == tableId))
            {
                playerState.HandCards = gamePlayers.FirstOrDefault(e => e.Name == user.Name)?.HandCards;
                Clients.Client(user.ConnectionId).SendAsync("ReceiveStateRefresh", playerState);
            }
        }
    }
    public void BlackjackPlayerStateRefresh(int tableId)
    {
        var playerState = new BlackjackPlayerStateModel();

        // Collect user states
        foreach (var user in Users.Where(e => e.TableId == tableId))
        {
            playerState.Players.Add(new GamePlayer
            {
                Username = user.Name,
                IsPlaying = user.InGame,
                IsReady = user.IsReady,
                SeatNumber = user.SeatNumber,
                GameMoney = user.Balance
            });

            // Determine current game (Blackjack)
            var blackjackGame = BlackjackGames.FirstOrDefault(e => e.TableId == tableId);

            if (blackjackGame != null && blackjackGame.Players.Select(e => e.Name).Contains(user.Name))
            {
                playerState.Players.Last().ActionState = PlayerActionState.Playing;
            }
        }

        // Set blackjack game states
        var activeBlackjackGame = BlackjackGames.FirstOrDefault(e => e.TableId == tableId);

        if (activeBlackjackGame != null)
        {
            playerState.GameInProgress = true; // assuming game is in progress if it's listed
        }

        var blackjackGamePlayers = activeBlackjackGame?.Players;

        // Send state refresh to all users based on the active blackjack game
        if (blackjackGamePlayers == null)
        {
            Clients.Group(tableId.ToString()).SendAsync("ReceiveBlackjackStateRefresh", playerState);
        }
        else
        {
            foreach (var user in Users.Where(e => e.TableId == tableId))
            {
                playerState.HandCards = blackjackGamePlayers.FirstOrDefault(e => e.Name == user.Name)?.HandCards;
                Clients.Client(user.ConnectionId).SendAsync("ReceiveBlackjackStateRefresh", playerState);
            }
        }
    }

    private int GetTableByUser(string name)
    {
        return Users.First(e => e.Name == name).TableId;
    }

    private bool IsUserInGameHub(string name)
    {
        return Users.Select(e => e.Name).Contains(name);
    }
}