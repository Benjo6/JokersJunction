using PokerTable = JokersJunction.Common.Databases.Models.PokerTable;

namespace JokersJunction.Server.Events.Response;

public class CurrentTableEventResponse
{
    public PokerTable Table { get; set; }
}