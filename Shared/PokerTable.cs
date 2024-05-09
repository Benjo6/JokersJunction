using MongoDB.Bson;

namespace JokersJunction.Shared;

public class PokerTable
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int MaxPlayers { get; set; }
    public int SmallBlind { get; set; }
}