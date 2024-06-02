
namespace JokersJunction.Shared;

public class PokerTable : UiTable
{
    public int SmallBlind { get; set; }
}

public class BlackjackTable : UiTable{}

public class UiTable 
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int MaxPlayers { get; set; }
}