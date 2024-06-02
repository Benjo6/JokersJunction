using JokersJunction.Common.Databases.Models;

namespace JokersJunction.Common.Events;

public class UserWithdrawEvent
{
    public User GameUser { get; set; }
}