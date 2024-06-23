namespace JokersJunction.Shared.Players;
public class PokerPlayer : Player
{
    public HandStrength HandStrength { get; set; } = HandStrength.HighCard;
    public PlayerActionState ActionState { get; set; } = PlayerActionState.Playing;
    public int RoundBet { get; set; }
}