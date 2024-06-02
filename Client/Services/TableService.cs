using JokersJunction.Shared;
using JokersJunction.Shared.Models;
using System.Net.Http.Json;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace JokersJunction.Client.Services
{
    public class TableService : ITableService
    {
        private readonly HttpClient _httpClient;

        public TableService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<CreateTableResult> Create(CreateTableModel model)
        {
            var response = await _httpClient.PostAsJsonAsync("gateway/table", model);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CreateTableResult>();
                return result;
            }
            else
            {
                // Handle error response here
                return null;
            }
        }
        public async Task<List<T>> GetList<T>() where T : UiTable
        {
            var result = await _httpClient.GetFromJsonAsync<List<T>>("api/table");
            return result;
        }


        public async Task<T> GetById<T>(int id) where T : UiTable
        {
            var result = await _httpClient.GetFromJsonAsync<T>($"api/table/{id}");
            return result;
        }

        public async Task<DeleteTableResult> Delete(string id)
        {
            var response = await _httpClient.PostAsJsonAsync($"gateway/table", id);

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

        private async Task<string> GetResponseContentAsync(string requestUri)
        {
            var response = await _httpClient.GetAsync(requestUri);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Request failed with status code: {response.StatusCode}");
            }

            return await response.Content.ReadAsStringAsync();
        }
    }
}
