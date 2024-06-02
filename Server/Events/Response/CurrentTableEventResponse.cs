using JokersJunction.Shared;
using BlackjackTable = JokersJunction.Common.Databases.Models.BlackjackTable;
using PokerTable = JokersJunction.Common.Databases.Models.PokerTable;

namespace JokersJunction.Server.Events.Response;

public class CurrentPokerTableEventResponse
{
    public PokerTable Table { get; set; }
}

public class CurrentBlackjackTableEventResponse
{
    public BlackjackTable Table { get; set; }
}