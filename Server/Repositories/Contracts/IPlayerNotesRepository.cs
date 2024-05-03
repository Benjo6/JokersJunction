using JokersJunction.Shared;

namespace JokersJunction.Server.Repositories.Contracts
{
    public interface IPlayerNotesRepository
    {
        public Task<IEnumerable<PlayerNote>> GetNotes(string userId);

        public Task<PlayerNote> AddNote(PlayerNote note);

        public Task<PlayerNote> DeleteNote(string userId, string notePlayerName);

        public Task<PlayerNote> GetNoteByName(string userId, string notePlayerName);

        public Task<PlayerNote> UpdateNote(PlayerNote playerNote);
    }
}
