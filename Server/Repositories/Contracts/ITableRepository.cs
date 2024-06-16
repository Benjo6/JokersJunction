using JokersJunction.Shared;

namespace JokersJunction.Server.Repositories.Contracts
{
    public interface ITableRepository
    {
        public Task<IEnumerable<PokerTable>> GetPokerTables();
        public Task<IEnumerable<BlackjackTable>> GetBlackjackTables();
        public Task<PokerTable?> GetPokerTableById(int tableId);
        public Task<BlackjackTable?> GetBlackjackTableById(int tableId);
        public Task<PokerTable?> GetPokerTableByName(string tableName);
        public Task<BlackjackTable?> GetBlackjackTableByName(string tableName);
        public Task<PokerTable> AddPokerTable(PokerTable table);
        public Task<BlackjackTable> AddBlackjackTable(BlackjackTable table);
        public Task<PokerTable> UpdatePokerTable(PokerTable table);
        public Task<BlackjackTable> UpdateBlackjackTable(BlackjackTable table);
        public Task<PokerTable> DeletePokerTable(int tableId);
        public Task<BlackjackTable> DeleteBlackjackTable(int tableId);

    }
}
