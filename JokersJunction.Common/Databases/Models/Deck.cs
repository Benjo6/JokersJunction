﻿using JokersJunction.Shared;

namespace JokersJunction.Common.Databases.Models;

public class Deck
{
    public Queue<Card> Cards { get; set; } = new();

    public Deck()
    {
        foreach (CardRank cardRank in Enum.GetValues(typeof(CardRank)))
        {
            foreach (CardSuit cardSuit in Enum.GetValues(typeof(CardSuit)))
            {
                Cards.Enqueue(new Card(cardRank, cardSuit));
            }
        }
        Shuffle();
    }

    public void Shuffle()
    {
        var rand = new Random();
        Cards = new Queue<Card>(Cards.OrderBy(a => rand.Next()));
    }

    public List<Card> DrawCards(int number)
    {
        return Enumerable.Range(0, number).Select(i => Cards.Dequeue()).ToList();
    }
}