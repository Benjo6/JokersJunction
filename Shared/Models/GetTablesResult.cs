namespace JokersJunction.Shared.Models
{
    public class GetTablesResult
    {
        public IEnumerable<PokerTable> PokerTables { get; set; }
        public bool Successful { get; set; }
        public string Error { get; set; }
    }
}
