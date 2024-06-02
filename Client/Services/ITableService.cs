using JokersJunction.Shared;
using JokersJunction.Shared.Models;

namespace JokersJunction.Client.Services
{
    public interface ITableService
    {
        Task<CreateTableResult> Create(CreateTableModel model);

        Task<GetTablesResult<T>> GetList<T>() where T : Table;

        Task<T> GetById<T>(int id) where T:Table;

        Task<DeleteTableResult> Delete(int id);
    }
}
