using JokersJunction.Common.Databases.Models;
using JokersJunction.Shared.Models;

namespace JokersJunction.Server.Responses;

public class PlayerStateResponse
{
    public string TableId { get; set; }
    public PlayerStateModel PlayerStateModel { get; set; }
    public List<Player>? GamePlayers { get; set; }
    public IEnumerable<User> TableUsers { get; set; }
}