using JokersJunction.Server;
using JokersJunction.Shared;
using JokersJunction.Shared.Data;
using JokersJunction.Table.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JokersJunction.Table.Repositories;

public class TableRepository : ITableRepository
{
    private readonly AppDbContext _appDbContext;

    public TableRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task<IEnumerable<PokerTable>> GetPokerTables()
    {
        return await _appDbContext.Set<PokerTable>().ToListAsync();
    }

    public async Task<IEnumerable<BlackjackTable>> GetBlackjackTables()
    {
        return await _appDbContext.Set<BlackjackTable>().ToListAsync();
    }

    public async Task<PokerTable?> GetPokerTableById(int tableId)
    {
        return await _appDbContext.Set<PokerTable>().FirstOrDefaultAsync(e => e.Id == tableId);
    }
    public async Task<BlackjackTable?> GetBlackjackTableById(int tableId)
    {
        return await _appDbContext.Set<BlackjackTable>().FirstOrDefaultAsync(e => e.Id == tableId);
    }

    public async Task<PokerTable?> GetPokerTableByName(string tableName)
    {
        return await _appDbContext.Set<PokerTable>().FirstOrDefaultAsync(e => e.Name == tableName);
    }

    public async Task<BlackjackTable?> GetBlackjackTableByName(string tableName)
    {
        return await _appDbContext.Set<BlackjackTable>().FirstOrDefaultAsync(e => e.Name == tableName);
    }

    public async Task<PokerTable> AddPokerTable(PokerTable table)
    {
        var result = await _appDbContext.Set<PokerTable>().AddAsync(table);
        await _appDbContext.SaveChangesAsync();
        return result.Entity;
    }
    public async Task<BlackjackTable> AddBlackjackTable(BlackjackTable table)
    {
        var result = await _appDbContext.Set<BlackjackTable>().AddAsync(table);
        await _appDbContext.SaveChangesAsync();
        return result.Entity;
    }

    public async Task<PokerTable> UpdatePokerTable(PokerTable table)
    {
        var result = await _appDbContext.Set<PokerTable>().FirstOrDefaultAsync(e => e.Id == table.Id);

        if (result == null) return null;

        result.MaxPlayers = table.MaxPlayers;
        result.Name = table.Name;
        result.SmallBlind = table.SmallBlind;
        return result;
    }

    public async Task<BlackjackTable> UpdateBlackjackTable(BlackjackTable table)
    {
        var result = await _appDbContext.Set<BlackjackTable>().FirstOrDefaultAsync(e => e.Id == table.Id);

        if (result == null) return null;

        result.MaxPlayers = table.MaxPlayers;
        result.Name = table.Name;
        return result;
    }

    public async Task<PokerTable> DeletePokerTable(int tableId)
    {
        var result = await _appDbContext.Set<PokerTable>().FirstOrDefaultAsync(e => e.Id == tableId);
        if (result == null) return null;

        _appDbContext.Set<PokerTable>().Remove(result);
        await _appDbContext.SaveChangesAsync();
        return result;
    }

    public async Task<BlackjackTable> DeleteBlackjackTable(int tableId)
    {
        var result = await _appDbContext.Set<BlackjackTable>().FirstOrDefaultAsync(e => e.Id == tableId);
        if (result == null) return null;

        _appDbContext.Set<BlackjackTable>().Remove(result);
        await _appDbContext.SaveChangesAsync();
        return result;
    }
}