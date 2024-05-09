using JokersJunction.Common.Databases.Models;
using MongoDB.Bson;

namespace JokersJunction.Table.Repositories.Interfaces
{
    public interface ITableRepository
    {
        public Task<List<PokerTable>> GetTables();

        public Task<PokerTable> GetTableById(string tableId);
        public Task<PokerTable?> GetTableByName(string tableName);

        public Task<PokerTable> AddTable(PokerTable table);

        public Task<PokerTable> UpdateTable(PokerTable table);

        public Task<bool> DeleteTable(string tableId);
    }
}
