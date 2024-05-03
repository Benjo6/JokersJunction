namespace JokersJunction.Shared.Responses
{
    public class CreateTableResponse
    {
        public bool Successful { get; set; }
        public IEnumerable<string> Errors { get; set; }
    }
}
