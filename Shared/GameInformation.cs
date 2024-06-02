namespace JokersJunction.Shared;

public class PokerGameInformation : GameInformation
{
    public List<GamePlayer> Players { get; set; }
    public List<Card> TableCards { get; set; }
    public List<Pot> Pots { get; set; }
    public bool GameInProgress { get; set; }
    public string CurrentPlayer { get; set; }
    public int SmallBlindIndex { get; set; }
    public int BigBlindIndex { get; set; }
    public string Winner { get; set; }
    public int RaiseAmount { get; set; }
    public int PlayerRaise { get; set; }
    public PokerGameInformation()
    {
        TableCards = new List<Card>();
        Hand = new List<Card>();
        Pots = new List<Pot>();
        GameInProgress = false;
        RaiseAmount = 0;
        PlayerRaise = 0;
    }

}
public class BlackjackGameInformation : GameInformation
{
    public List<GamePlayer> Players { get; set; } = new();
    public List<Card> DealerHand { get; set; } = new();
    public bool GameInProgress { get; set; }
    public string CurrentPlayer { get; set; }
    public int PlayerBet { get; set; }
    public string Winner { get; set; }
}

public class GameInformation
{
    public List<Card> Hand { get; set; } = new();
    public List<PlayerNote> PlayersNotes { get; set; } = new();
}