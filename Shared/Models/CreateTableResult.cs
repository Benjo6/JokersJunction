namespace JokersJunction.Shared.Models
{
    public class CreateTableResult
    {
        public bool Successful { get; set; }
        public IEnumerable<string> Errors { get; set; }
    }
}
