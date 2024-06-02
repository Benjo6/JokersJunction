using JokersJunction.Common.Databases.Models;

namespace JokersJunction.GameManagement.Services.Contracts;

public interface IGameService
{
    public Task BlackjackPlayerStateRefresh(string tableId);
    public Task PokerPlayerStateRefresh(string tableId, List<PokerGame>? games);
    public Task StartGame(string tableId, int smallBlindPosition, List<User> users);
    public Task MoveIndex(string tableId, PokerGame currentGame); 
    public Task UpdatePot(PokerGame currentGame);
    public Task GetAndAwardWinners(PokerGame currentGame);
    public Task CompleteBlackjackGame(string tableId);
}