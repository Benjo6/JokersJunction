using JokersJunction.Shared;
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
            httpClient.BaseAddress = new Uri("https://localhost:2000/");
            _httpClient = httpClient;
        }

        public async Task<CreateNoteResult> Create(CreateNoteModel model)
        {
            var response = await _httpClient.PostAsJsonAsync("gateway/player-note", model);

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

        public async Task<GetNotesResult> GetList(string userId)
        {
            var result = await _httpClient.GetFromJsonAsync<GetNotesResult>($"gateway/player-note/{userId}");
            return result;
        }

        public async Task<DeleteTableResult> Delete(string userId, string notedPlayerName)
        {
            var response = await _httpClient.DeleteAsync($"gateway/player-note/{userId}/{notedPlayerName}");

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

        public async Task<PlayerNote> GetNoteByName(string userId, string notePlayerName)
        {
            var result = await _httpClient.GetFromJsonAsync<PlayerNote>($"gateway/player-note/by-name/{userId}/{notePlayerName}");
            return result;
        }
    }
}
