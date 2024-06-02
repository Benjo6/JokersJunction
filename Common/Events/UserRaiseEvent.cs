using JokersJunction.Common.Databases.Models;

namespace JokersJunction.Common.Events;

public class UserRaiseEvent
{
    public User GameUser { get; set; }
    public int Amount { get; set; }
}