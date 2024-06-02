using JokersJunction.Common.Databases.Models;

namespace JokersJunction.Common.Events;

public class UserInGameEvent
{
    public User User { get; set; }
}