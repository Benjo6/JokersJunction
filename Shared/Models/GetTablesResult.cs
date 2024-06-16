namespace JokersJunction.Shared.Models
{
    public class GetPokerTablesResult
    {
        public IEnumerable<PokerTable> Tables { get; set; } 
        public bool Successful { get; set; }
        public string Error { get; set; }
    }

    public class GetBlackjackTablesResult
    {
        public IEnumerable<BlackjackTable> Tables { get; set; }
        public bool Successful { get; set; }
        public string Error { get; set; }
    }
}
