using JokersJunction.Shared;

namespace JokersJunction.Server.Models;

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

    public PokerGame(int tableId, int smallBlindIndex, int smallBlind)
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