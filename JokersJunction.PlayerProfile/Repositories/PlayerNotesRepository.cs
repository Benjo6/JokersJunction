using JokersJunction.PlayerProfile.Repositories.Interfaces;
using JokersJunction.Server.Data;
using JokersJunction.Shared;
using JokersJunction.Shared.Data;
using JokersJunction.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace JokersJunction.PlayerProfile.Repositories;

internal class PlayerNotesRepository : IPlayerNotesRepository
{
    private readonly AppDbContext _appDbContext;
    public PlayerNotesRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task<List<PlayerNote>> GetAsync(string userId)
    {
        return await _appDbContext.PlayerNotes
            .Where(e => e.UserId == userId)
            .ToListAsync();
    }

    public async Task<PlayerNote?> GetByNameAsync(string userId, string notePlayerName)
    {
        return await _appDbContext.PlayerNotes
            .FirstOrDefaultAsync(e => e!.UserId == userId && e.NotedPlayerName == notePlayerName);
    }

    public async Task<PlayerNote?> AddAsync(CreateNoteModel? note)
    {
        var newPlayerNote = new PlayerNote
        {
            NotedPlayerName = note!.NotedPlayerName,
            Description = note.Description,
            UserId = note.UserId
        };

        var update = await UpdateAsync(newPlayerNote);
        if (update != null) return null;
        var result = await _appDbContext.PlayerNotes.AddAsync(newPlayerNote);
        await SaveChangesAsync();
        return result.Entity;
    }

    public async Task<PlayerNote?> DeleteAsync(string userId, string notePlayerName)
    {
        var result = await _appDbContext.PlayerNotes
            .FirstOrDefaultAsync(e => e!.UserId == userId && e.NotedPlayerName == notePlayerName);

        if (result == null)
            return null;

        _appDbContext.PlayerNotes.Remove(result);
        await SaveChangesAsync();
        return result;
    }

    public async Task<PlayerNote?> UpdateAsync(PlayerNote? playerNote)
    {
        var existingNote = await _appDbContext.PlayerNotes
            .FirstOrDefaultAsync(e => e!.UserId == playerNote!.UserId && e.NotedPlayerName == playerNote.NotedPlayerName);

        if (existingNote == null)
            return null;

        _appDbContext.PlayerNotes.Remove(existingNote);
        _appDbContext.PlayerNotes.Add(playerNote);
        await SaveChangesAsync();

        return existingNote;
    }

    private async Task SaveChangesAsync()
    {
        await _appDbContext.SaveChangesAsync();
    }
}