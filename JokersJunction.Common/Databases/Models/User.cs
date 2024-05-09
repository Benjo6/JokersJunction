using JokersJunction.Common.Databases.Base;
using MongoDB.Bson;

namespace JokersJunction.Common.Databases.Models;

public class User : Document
{
    public string ConnectionId { get; set; }
    public string TableId { get; set; }
    public bool IsReady { get; set; } = false;
    public bool InGame { get; set; }
    public int SeatNumber { get; set; }
    public int Balance { get; set; }
}