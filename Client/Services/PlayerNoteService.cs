using JokersJunction.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using JokersJunction.Shared;

namespace JokersJunction.Client.Services
{
    public class PlayerNoteService : IPlayerNoteService
    {
        private readonly HttpClient _httpClient;

        public PlayerNoteService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<CreateNoteResult> Create(CreatePlayerNote model)
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

        public async Task<GetNotesResult> GetList(string? userId)
        {
            var result = await _httpClient.GetFromJsonAsync<GetNotesResult>($"api/PlayerNote?userId={userId}");
            return result;
        }


        public async Task<DeleteTableResult> Delete(string userId, string notedPlayerName)
        {
            var requestPayload = new { userId, notedPlayerName }; // Create an anonymous object with both parameters

            var response = await _httpClient.PostAsJsonAsync("api/PlayerNote/delete", requestPayload);

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
