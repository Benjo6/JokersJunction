using JokersJunction.Shared;
using JokersJunction.Shared.Models;
using JokersJunction.Shared.Requests;
using JokersJunction.Shared.Responses;

namespace JokersJunction.Client.Services
{
    public interface ITableService
    {
        Task<CreateTableResponse?> Create(CreateTableRequest request);

        Task<GetTablesResult?> GetList();

        Task<PokerTable?> GetById(int id);

        Task<DeleteTableResult?> Delete(int id);
    }
}
