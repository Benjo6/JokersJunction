using JokersJunction.Common.Databases.Base;

namespace JokersJunction.Common.Databases.Models;

public class PokerTable : Document
{
    public int MaxPlayers { get; set; }
    public int SmallBlind { get; set; }
}