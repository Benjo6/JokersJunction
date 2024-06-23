using JokersJunction.Server;
using JokersJunction.Shared;
using JokersJunction.Shared.Models;

namespace JokersJunction.Client.Services
{
    public interface ITableService
    {
        Task<CreateTableResult> CreatePoker(CreateTableModel model);

        Task<CreateTableResult> CreateBlackjack(CreateTableModel model);

        Task<GetBlackjackTablesResult> GetBlackjackList();

        Task<GetPokerTablesResult> GetPokerList();

        Task<PokerTable> GetByPokerId(int id);
        Task<BlackjackTable> GetByBlackjackId(int id);

        Task<DeleteTableResult> DeletePoker(int id);
        Task<DeleteTableResult> DeleteBlackjack(int id);
    }
}
