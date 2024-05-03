using JokersJunction.Shared;
using JokersJunction.Shared.Models;

namespace JokersJunction.Client.Services
{
    public interface IPlayerNoteService
    {
        Task<CreateNoteResult> Create(CreatePlayerNote model);

        Task<GetNotesResult> GetList(string? userId);

        Task<DeleteTableResult> Delete(string userId, string notedPlayerName);
    }
}
