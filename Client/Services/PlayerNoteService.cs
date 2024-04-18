using JokersJunction.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace JokersJunction.Client.Services
{
    public class PlayerNoteService : IPlayerNoteService
    {
        private readonly HttpClient _httpClient;

        public PlayerNoteService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<CreateNoteResult> Create(CreateNoteModel model)
        {
            var response = await _httpClient.PostAsJsonAsync("api/PlayerNote", model);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CreateNoteResult>();
                return result;
            }
            else
            {
                // Handle error response here
                return null;
            }
        }

        public async Task<GetNotesResult> GetList()
        {
            var result = await _httpClient.GetFromJsonAsync<GetNotesResult>("api/PlayerNote");
            return result;
        }

        public async Task<DeleteTableResult> Delete(string notedPlayerName)
        {
            var response = await _httpClient.PostAsJsonAsync("api/PlayerNote/delete", notedPlayerName);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<DeleteTableResult>();
                return result;
            }
            else
            {
                // Handle error response here
                return null;
            }
        }
    }
}
