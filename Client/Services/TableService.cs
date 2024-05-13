using JokersJunction.Shared;
using JokersJunction.Shared.Models;
using System.Net.Http.Json;
using Newtonsoft.Json;

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
        public async Task<List<PokerTable>> GetList()
        {
            try
            {
                var responseContent = await GetResponseContentAsync("gateway/table");
                if (string.IsNullOrEmpty(responseContent))
                {
                    throw new Exception("Received empty response content.");
                }

                var tables = JsonConvert.DeserializeObject<List<PokerTable>>(responseContent); 
                return tables ?? throw new Exception("Failed to deserialize the response content.");
            }
            catch (HttpRequestException e)
            {
                // Handle HTTP request-specific exceptions e.g., connectivity issues, timeouts.
                Console.WriteLine($"HTTP Request failed: {e.Message}");
                throw;
            }
            catch (Exception e)
            {
                // Handle non-HTTP exceptions.
                Console.WriteLine($"An error occurred: {e.Message}");
                throw;
            }
        }


        public async Task<PokerTable> GetById(string id)
        {
            var result = await _httpClient.GetFromJsonAsync<PokerTable>($"gateway/table/{id}");
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
