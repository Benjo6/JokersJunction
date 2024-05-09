using AutoMapper;
using JokersJunction.Shared.Models;

namespace JokersJunction.Table;

public class PokerTableProfile : Profile
{
    public PokerTableProfile()
    {
        CreateMap<CreateTableModel, Common.Databases.Models.PokerTable>();
        CreateMap<Common.Databases.Models.PokerTable, Shared.PokerTable>();
        CreateMap<List<Shared.PokerTable>, List<Common.Databases.Models.PokerTable>>();
        CreateMap<List<Common.Databases.Models.PokerTable>, List<Shared.PokerTable>>();
    }
}