using JokersJunction.Common.Databases.Models;

namespace JokersJunction.Table.Repositories.Interfaces
{
    public interface ITableRepository
    {
        public Task<List<T>> GetTables<T>() where T : Common.Databases.Models.Inheritor.Table;
        public Task<T> GetTableById<T>(string tableId) where T : Common.Databases.Models.Inheritor.Table;
        public Task<T?> GetTableByName<T>(string tableName) where T : Common.Databases.Models.Inheritor.Table;

        public Task<T> AddTable<T>(T table) where T : Common.Databases.Models.Inheritor.Table;

        public Task<T> UpdateTable<T>(T table) where T : Common.Databases.Models.Inheritor.Table;

        public Task<bool> DeleteTable(string tableId);
    }
}
