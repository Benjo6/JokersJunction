using JokersJunction.Common.Databases.Models;

namespace JokersJunction.Common.Events.Responses;

public class UserIsReadyEventResponse
{
    public User? User { get; set; }
}