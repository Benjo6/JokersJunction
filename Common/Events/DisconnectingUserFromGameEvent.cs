namespace JokersJunction.Common.Events;

public class DisconnectingUserFromGameEvent
{
    public string UserName { get; set; }
    public string TableId { get; set; }
}