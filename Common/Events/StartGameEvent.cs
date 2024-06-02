namespace JokersJunction.Common.Events;

public class StartGameEvent
{
    public string TableId { get; set; }
    public int SmallBlindPosition { get; set; }

}