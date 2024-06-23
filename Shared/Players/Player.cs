namespace JokersJunction.Shared.Players;

public class Player
{
    public string Name { get; set; }
    public List<Card> HandCards { get; set; } = new List<Card>();

}