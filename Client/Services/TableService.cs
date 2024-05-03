using JokersJunction.Shared;
using JokersJunction.Shared.Models;
using System.Net.Http.Json;

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
            var response = await _httpClient.PostAsJsonAsync("api/table", model);

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

        public async Task<GetTablesResult> GetList()
        {
            var result = await _httpClient.GetFromJsonAsync<GetTablesResult>("api/table");
            return result;
        }

        public async Task<PokerTable> GetById(int id)
        {
            var result = await _httpClient.GetFromJsonAsync<PokerTable>($"api/table/{id}");
            return result;
        }

        public async Task<DeleteTableResult> Delete(int id)
        {
            var response = await _httpClient.PostAsJsonAsync($"api/table/delete", id);

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
