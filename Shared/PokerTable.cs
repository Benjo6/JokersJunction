﻿
namespace JokersJunction.Shared;
public class PokerTable
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int MaxPlayers { get; set; }
    public int SmallBlind { get; set; }
}



public class BlackjackTable
{
    public int Id { get; set; }

    public string Name { get; set; }

    public int MaxPlayers { get; set; }
}