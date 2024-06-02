using JokersJunction.Common.Databases.Models;

namespace JokersJunction.Common.Events.Responses;

public class GetUserByNameEventResponse
{
    public User GameUser { get; set; }
}