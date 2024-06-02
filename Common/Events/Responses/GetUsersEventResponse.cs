using JokersJunction.Common.Databases.Models;

namespace JokersJunction.Common.Events.Responses;

public class GetUsersEventResponse
{
    public List<User> Users { get; set; } = new();
}