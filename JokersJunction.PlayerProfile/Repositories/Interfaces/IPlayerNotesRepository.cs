using JokersJunction.Shared;
using JokersJunction.Shared.Models;

namespace JokersJunction.PlayerProfile.Repositories.Interfaces;

public interface IPlayerNotesRepository
{
    public Task<List<PlayerNote>> GetAsync(string userId);

    public Task<PlayerNote?> AddAsync(CreateNoteModel? note);

    public Task<PlayerNote?> DeleteAsync(string userId, string notePlayerName);

    public Task<PlayerNote?> GetByNameAsync(string userId, string notePlayerName);

    public Task<PlayerNote?> UpdateAsync(PlayerNote? playerNote);
}