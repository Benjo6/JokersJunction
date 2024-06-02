using JokersJunction.Common.Databases.Models;

namespace JokersJunction.Common.Events;

public class StartBlindEvent
{
    public string SmallBlindName { get; set; } = string.Empty;
    public int SmallBlind { get; set; }
    public string BigBlindName { get; set; }
    public int BigBlind { get; set; }
    public List<Player> Players { get; set; } = new();
}