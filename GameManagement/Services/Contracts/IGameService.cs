using JokersJunction.Common.Databases.Models;

namespace JokersJunction.GameManagement.Services.Contracts;

public interface IGameService
{
    public Task PlayerStateRefresh(string tableId, List<Game>? games);
    public Task StartGame(string tableId, int smallBlindPosition, List<User> users);
    public Task MoveIndex(string tableId, Game currentGame); 
    public Task UpdatePot(Game currentGame);
    public Task GetAndAwardWinners(Game currentGame);

}