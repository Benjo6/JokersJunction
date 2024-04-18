using AutoMapper;
using JokersJunction.Shared;
using JokersJunction.Shared.Models;

namespace JokersJunction.Server.Models
{
    public class PokerTableProfile : Profile
    {
        public PokerTableProfile()
        {
            CreateMap<CreateTableModel, PokerTable>();
        }
    }
}
