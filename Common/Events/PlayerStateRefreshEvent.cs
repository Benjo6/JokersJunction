using JokersJunction.Common.Databases.Models;

namespace JokersJunction.Common.Events;

public class PokerPlayerStateRefreshEvent
{
    public string TableId { get; set; }
    public List<PokerGame>? Games { get; set; }
}

public class BlackjackPlayerStateRefreshEvent
{
    public string TableId { get; set; }
    public List<BlackjackGame>? Games { get; set; }
}