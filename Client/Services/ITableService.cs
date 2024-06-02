using JokersJunction.Shared;
using JokersJunction.Shared.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace JokersJunction.Client.Services
{
    public interface ITableService
    {
        Task<CreateTableResult> Create(CreateTableModel model);

        Task<List<T>> GetList<T>() where T : UiTable;

        Task<T> GetById<T>(int id) where T : UiTable;

        Task<DeleteTableResult> Delete(string id);
    }
}
