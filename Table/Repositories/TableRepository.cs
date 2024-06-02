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
        public async Task<List<PokerTable>> GetTables()
        {
            return await _databaseService.ReadAsync<PokerTable>();
        }

        public async Task<PokerTable?> GetTableById(string tableId)
        {
            return await _databaseService.GetOneFromIdAsync<PokerTable>(tableId);
        }

        public async Task<PokerTable?> GetTableByName(string tableName)
        {
            return await _databaseService.GetOneByNameAsync<PokerTable>(tableName);
        }

        public async Task<PokerTable> AddTable(PokerTable table)
        {
            _databaseService.InsertOne(table);
            return await _databaseService.GetOneByNameAsync<PokerTable>(table.Name);
        }

        public async Task<PokerTable> UpdateTable(PokerTable table)
        {
            await _databaseService.ReplaceOneAsync(table);
            return await _databaseService.GetOneByNameAsync<PokerTable>(table.Name);
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