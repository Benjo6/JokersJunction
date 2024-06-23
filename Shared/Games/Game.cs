namespace JokersJunction.Shared.Games;

public class Game
{
    public int TableId { get; set; }
    public Deck Deck { get; set; } = new();

}