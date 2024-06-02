using JokersJunction.Common.Databases.Models;
using JokersJunction.Shared.Models;

namespace JokersJunction.Server.Responses;

public class PokerPlayerStateResponse
{
    public string TableId { get; set; }
    public PokerPlayerStateModel PlayerStateModel { get; set; }
    public List<PokerPlayer>? GamePlayers { get; set; }
    public IEnumerable<User> TableUsers { get; set; }
}
public class BlackjackPlayerStateResponse
{
    public string TableId { get; set; }
    public BlackjackPlayerStateModel PlayerStateModel { get; set; }
    public List<BlackjackPlayer>? GamePlayers { get; set; }
    public IEnumerable<User> TableUsers { get; set; }
}