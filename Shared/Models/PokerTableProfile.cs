using AutoMapper;
using JokersJunction.Shared.Requests;

namespace JokersJunction.Shared.Models;

public class PokerTableProfile : Profile
{
    public PokerTableProfile()
    {
        CreateMap<CreateTableRequest, PokerTable>();
    }
}