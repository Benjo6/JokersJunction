using JokersJunction.Common.Databases.Models;

namespace JokersJunction.Common.Events;

public class PlayerStateRefreshEvent
{
    public string TableId { get; set; }
    public List<Game>? Games { get; set; }
}