using JokersJunction.Common.Databases.Base;
using JokersJunction.Shared;
using MongoDB.Bson;

namespace JokersJunction.Common.Databases.Models;

public class Player
{
    public string? Name { get; set; }
    public List<Card>? HandCards { get; set; } = [];
    public HandStrength HandStrength { get; set; } = HandStrength.HighCard;
    public PlayerActionState ActionState { get; set; } = PlayerActionState.Playing;
    public int RoundBet { get; set; }
}