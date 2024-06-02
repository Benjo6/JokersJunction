using JokersJunction.Common.Databases.Interfaces;
using JokersJunction.Common.Databases.Models;
using JokersJunction.Table.Repositories.Interfaces;

namespace JokersJunction.Table.Repositories
{
    public class TableRepository : ITableRepository
    {
        private readonly IDatabaseService _databaseService;
        public TableRepository(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }
        public async Task<List<T>> GetTables<T>() where T : Common.Databases.Models.Inheritor.Table
        {
            return await _databaseService.ReadAsync<T>();
        }

        public async Task<T?> GetTableById<T>(string tableId) where T : Common.Databases.Models.Inheritor.Table
        {
            return await _databaseService.GetOneFromIdAsync<T>(tableId);
        }

        public async Task<T?> GetTableByName<T>(string tableName) where T : Common.Databases.Models.Inheritor.Table
        {
            return await _databaseService.GetOneByNameAsync<T>(tableName);
        }

        public async Task<T> AddTable<T>(T table) where T : Common.Databases.Models.Inheritor.Table
        {
            _databaseService.InsertOne(table);
            return await _databaseService.GetOneByNameAsync<T>(table.Name);
        }

        public async Task<T> UpdateTable<T>(T table) where T : Common.Databases.Models.Inheritor.Table
        {
            await _databaseService.ReplaceOneAsync(table);
            return await _databaseService.GetOneByNameAsync<T>(table.Name);
        }

        public async Task<bool> DeleteTable(string tableId)
        {
            var itemToDelete = await _databaseService.GetOneFromIdAsync<PokerTable>(tableId);
            if (itemToDelete is null)
            {
                return false;
            }
            return await _databaseService.DeleteOneAsync(itemToDelete);
        }
    }
}