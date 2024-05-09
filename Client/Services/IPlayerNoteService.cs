using JokersJunction.Shared;
using JokersJunction.Shared.Models;

namespace JokersJunction.Client.Services
{
    public interface IPlayerNoteService
    {
        Task<CreateNoteResult> Create(CreateNoteModel model);

        Task<GetNotesResult> GetList(string userId);

        Task<PlayerNote> GetNoteByName(string userId, string notePlayerName);


        Task<DeleteTableResult> Delete(string userId, string notedPlayerName);
    }
}
