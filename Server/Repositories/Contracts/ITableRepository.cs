using JokersJunction.Shared;

namespace JokersJunction.Server.Repositories.Contracts
{
    public interface ITableRepository
    {
        public Task<IEnumerable<T>> GetTables<T>() where T : Table;

        public Task<T> GetTableById<T>(int tableId) where T:Table;
        public Task<T> GetTableByName<T>(string tableName) where T:Table;

        public Task<T> AddTable<T>(T table) where T:Table;

        public Task<T> UpdateTable<T>(T table) where T:Table;

        public Task<T> DeleteTable<T>(int tableId) where T:Table;
    }
}
