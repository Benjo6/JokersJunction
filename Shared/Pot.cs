namespace JokersJunction.Shared
{
    public class Pot
    {
        public HashSet<string> Players { get; set; } = new();
        public int PotAmount { get; set; }
        public string Winner { get; set; }
    }
}
