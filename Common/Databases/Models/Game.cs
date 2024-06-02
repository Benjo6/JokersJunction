using JokersJunction.Common.Databases.Base;
using JokersJunction.Shared;
using MongoDB.Bson;

namespace JokersJunction.Common.Databases.Models;

public class Game : Document
{
    public string TableId { get; set; }
    public Deck Deck { get; set; } = new();
}

public class PokerGame : Game
{
    public List<Card> TableCards { get; set; }

    public List<PokerPlayer> Players { get; set; }
    public int Index { get; set; }

    public int RoundEndIndex { get; set; }

    public int SmallBlindIndex { get; set; }

    public int BigBlindIndex { get; set; }

    public int RaiseAmount { get; set; }

    public int SmallBlind { get; set; }

    public List<Pot> Winnings { get; set; }

    public CommunityCardsActions CommunityCardsActions { get; set; }

    public PokerGame(string tableId, int smallBlindIndex, int smallBlind)
    {
        TableId = tableId;
        Players = new List<PokerPlayer>();
        TableCards = new List<Card>();
        Winnings = new List<Pot>();
        Deck = new Deck();
        SmallBlindIndex = smallBlindIndex;
        BigBlindIndex = smallBlindIndex + 1;
        RoundEndIndex = smallBlindIndex + 2;
        Index = smallBlindIndex + 2;
        CommunityCardsActions = CommunityCardsActions.PreFlop;
        SmallBlind = smallBlind;
        RaiseAmount = SmallBlind * 2;
    }

    public int NormalizeIndex(int index)
    {
        return index % Players.Count;
    }

    public void NormalizeAllIndexes()
    {
        SmallBlindIndex = NormalizeIndex(SmallBlindIndex);
        BigBlindIndex = NormalizeIndex(BigBlindIndex);
        RoundEndIndex = NormalizeIndex(RoundEndIndex);
        Index = NormalizeIndex(Index);
    }

    public void SetIndex(int index)
    {
        Index = NormalizeIndex(index);
    }

    public void SetRoundEndIndex(int index)
    {
        RoundEndIndex = NormalizeIndex(index);
    }

    public string GetPlayerNameByIndex(int index)
    {
        return Players.ElementAt(index).Name;
    }

    public PokerPlayer GetPlayerByIndex(int index)
    {
        return Players.ElementAt(index);
    }
}
public class BlackjackGame : Game
{
    public List<BlackjackPlayer> Players { get; set; } = new();
    public List<Card> DealerHand { get; set; } = new();
    public bool IsGameOver { get; set; } = false;
    public int InitialBet { get; set; }

    public BlackjackGame(string tableId, int initialBet)
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
