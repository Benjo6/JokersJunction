using JokersJunction.Shared.Models;

namespace JokersJunction.Client.Services
{
    public interface IPlayerNoteService
    {
        Task<CreateNoteResult> Create(CreateNoteModel model);

        Task<GetNotesResult> GetList();

        Task<DeleteTableResult> Delete(string notedPlayerName);
    }
}
