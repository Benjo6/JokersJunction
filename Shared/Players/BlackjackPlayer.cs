namespace JokersJunction.Shared.Players;

public class BlackjackPlayer : Player
{
    public int RoundBet { get; set; }
    public bool IsBust { get; set; } = false;
    public bool IsStand { get; set; } = false;

    public int GetHandValue()
    {
        var value = 0;
        var aceCount = 0;

        foreach (var card in HandCards)
        {
            if (card.CardNumber == CardRank.Ace)
            {
                aceCount++;
                value += 11;
            }
            else if (card.CardNumber == CardRank.King || card.CardNumber == CardRank.Queen || card.CardNumber == CardRank.Jack)
            {
                value += 10;
            }
            else
            {
                value += (int)card.CardNumber + 1;
            }
        }

        while (value > 21 && aceCount > 0)
        {
            value -= 10;
            aceCount--;
        }

        return value;
    }
}