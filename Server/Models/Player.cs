using JokersJunction.Shared;

namespace JokersJunction.Server.Models;

public class Player
{
    public string Name { get; set; }
    public List<Card> HandCards { get; set; } = new List<Card>();

}