using JokersJunction.Common.Databases.Models.Inheritor;

namespace JokersJunction.Common.Databases.Models;

public class PokerTable : Table
{
    public int SmallBlind { get; set; }
}

public class BlackjackTable : Table;