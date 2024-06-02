using JokersJunction.Server.Data;
using JokersJunction.Server.Repositories.Contracts;
using JokersJunction.Shared;
using Microsoft.EntityFrameworkCore;

namespace JokersJunction.Server.Repositories
{
    public class TableRepository : ITableRepository
    {
        private readonly AppDbContext _appDbContext;

        public TableRepository(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<IEnumerable<T>> GetTables<T>() where T : Table
        {
            return await _appDbContext.Set<T>().ToListAsync();
        }


        public async Task<T> GetTableById<T>(int tableId) where T: Table
        {
            return await _appDbContext.Set<T>().FirstOrDefaultAsync(e => e.Id == tableId);
        }

        public async Task<T> GetTableByName<T>(string tableName) where T: Table
        {
            return await _appDbContext.Set<T>().FirstOrDefaultAsync(e => e.Name == tableName);
        }

        public async Task<T> AddTable<T>(T table) where T: Table
        {
            var result = await _appDbContext.Set<T>().AddAsync(table);
            await _appDbContext.SaveChangesAsync();
            return result.Entity;
        }

        public async Task<T> UpdateTable<T>(T table) where T : Table
        {
            var result = await _appDbContext.Set<T>().FirstOrDefaultAsync(e => e.Id == table.Id);

            if (result == null) return null;

            result.MaxPlayers = table.MaxPlayers;
            result.Name = table.Name;

            return result;

        }

        public async Task<T> DeleteTable<T>(int tableId) where T: Table
        {
            var result = await _appDbContext.Set<T>().FirstOrDefaultAsync(e => e.Id == tableId);
            if (result == null) return null;

            _appDbContext.Set<T>().Remove(result);
            await _appDbContext.SaveChangesAsync();
            return result;
        }
    }
}