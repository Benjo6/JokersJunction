using JokersJunction.Server.Data;
using JokersJunction.Shared;
using JokersJunction.Table.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JokersJunction.Table.Repositories;

public class TableRepository : ITableRepository
{
    private readonly AppDbContext _appDbContext;
    private readonly ILogger<TableRepository> _logger;

    public TableRepository(AppDbContext appDbContext, ILogger<TableRepository> logger)
    {
        _appDbContext = appDbContext ?? throw new ArgumentNullException(nameof(appDbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<PokerTable>> GetTables()
    {
        try
        {
            _logger.LogInformation("Fetching poker tables.");
            return await _appDbContext.PokerTables.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching poker tables.");
            throw; // Rethrow the exception for higher-level handling
        }
    }

    public async Task<PokerTable> GetTableById(int tableId)
    {
        try
        {
            return await _appDbContext.PokerTables.SingleOrDefaultAsync(e => e.Id == tableId) ?? throw new TableNotFoundException($"{tableId} not found in the table");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching poker table with ID {tableId}.");
            throw;
        }
    }

    public async Task<PokerTable> GetTableByName(string tableName)
    {
        try
        {
            return await _appDbContext.PokerTables.SingleOrDefaultAsync(e => e.Name == tableName) ?? throw new TableNotFoundException($"{tableName} not found in the table");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching poker table with name {tableName}.");
            throw;
        }
    }

    public async Task<PokerTable> AddTable(PokerTable table)
    {
        try
        {
            var result = await _appDbContext.PokerTables.AddAsync(table);
            await _appDbContext.SaveChangesAsync();
            return result.Entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding poker table.");
            throw;
        }
    }

    public async Task<PokerTable?> UpdateTable(PokerTable table)
    {
        try
        {
            var existingTable = await _appDbContext.PokerTables.SingleOrDefaultAsync(e => e.Id == table.Id);
            if (existingTable == null)
                return null;

            existingTable.MaxPlayers = table.MaxPlayers;
            existingTable.Name = table.Name;

            await _appDbContext.SaveChangesAsync();
            return existingTable;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating poker table with ID {table.Id}.");
            throw;
        }
    }

    public async Task<PokerTable?> DeleteTable(int tableId)
    {
        try
        {
            var existingTable = await _appDbContext.PokerTables.SingleOrDefaultAsync(e => e.Id == tableId);
            if (existingTable == null)
                return null;

            _appDbContext.PokerTables.Remove(existingTable);
            await _appDbContext.SaveChangesAsync();
            return existingTable;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting poker table with ID {tableId}.");
            throw;
        }
    }
    private class TableNotFoundException : Exception
    {
        public TableNotFoundException(string message) : base(message) { }
    }
}