using JokersJunction.Common.Databases.Base;

namespace JokersJunction.Common.Databases.Models.Inheritor;

public class Table : Document
{
    public int MaxPlayers { get; set; }
}