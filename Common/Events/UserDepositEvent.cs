using JokersJunction.Common.Databases.Models;

namespace JokersJunction.Common.Events;

public class UserDepositEvent
{
    public User User { get; set; }
    public int Amount { get; set; }
}