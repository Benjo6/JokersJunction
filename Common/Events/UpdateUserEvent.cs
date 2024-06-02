using JokersJunction.Common.Databases.Models;

namespace JokersJunction.Common.Events;

public class UpdateUserEvent
{
    public User GameUser { get; set; }
}