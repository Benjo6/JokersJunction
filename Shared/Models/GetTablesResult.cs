namespace JokersJunction.Shared.Models
{
    public class GetTablesResult<T> where T : Table
    {
        public IEnumerable<T> Tables { get; set; } 
        public bool Successful { get; set; }
        public string Error { get; set; }
    }
}
