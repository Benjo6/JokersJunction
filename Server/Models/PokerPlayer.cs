using JokersJunction.Shared;

namespace JokersJunction.Server.Models;

public class PokerPlayer : Player
{
    public HandStrength HandStrength { get; set; } = HandStrength.HighCard;
    public PlayerActionState ActionState { get; set; } = PlayerActionState.Playing;
    public int RoundBet { get; set; }
}