namespace JokersJunction.Shared.Models;

public class PokerPlayerStateModel
{
    public List<GamePlayer> Players { get; set; } = new();
    public List<Card>? CommunityCards { get; set; } = new();
    public List<Card>? HandCards { get; set; } = new();
    public List<Pot>? Pots { get; set; } = new();
    public bool GameInProgress { get; set; }
    public int RaiseAmount { get; set; } = 0;
    public int SmallBlind { get; set; } = 0;
}

public class BlackjackPlayerStateModel
{
    public List<GamePlayer> Players { get; set; } = new();
    public List<Card> HandCards { get; set; } = new();
    public bool GameInProgress { get; set; }
}