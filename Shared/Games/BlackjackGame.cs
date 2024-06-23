using JokersJunction.Shared.Players;

namespace JokersJunction.Shared.Games;

public class BlackjackGame : Game
{
    public List<BlackjackPlayer> Players { get; set; } = new();
    public List<Card> DealerHand { get; set; } = new();
    public bool IsGameOver { get; set; } = false;
    public int InitialBet { get; set; }

    public BlackjackGame(int tableId, int initialBet)
    {
        TableId = tableId;
        InitialBet = initialBet;
        Deck.Shuffle();
        DealerHand.AddRange(Deck.DrawCards(2));
    }

    public int GetDealerHandValue()
    {
        var value = 0;
        var aceCount = 0;

        foreach (var card in DealerHand)
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
                value += (int)card.CardNumber;
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