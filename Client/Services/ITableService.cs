using JokersJunction.Shared;
using JokersJunction.Shared.Models;

namespace JokersJunction.Client.Services
{
    public interface ITableService
    {
        Task<CreateTableResult> Create(CreateTableModel model);

        Task<List<PokerTable>> GetList();

        Task<PokerTable> GetById(string id);

        Task<DeleteTableResult> Delete(string id);
    }
}
