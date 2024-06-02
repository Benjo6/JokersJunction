namespace JokersJunction.Shared
{
    public class Table
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int MaxPlayers { get; set; }
    }


    public class PokerTable : Table
    {
        public int SmallBlind { get; set; }
    }

    public class BlackjackTable : Table
    { }
}
