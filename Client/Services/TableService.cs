using JokersJunction.Shared;
using JokersJunction.Shared.Models;
using JokersJunction.Shared.Requests;
using JokersJunction.Shared.Responses;
using System.Net.Http.Json;
using System.Text.Json;

namespace JokersJunction.Client.Services;

public class TableService : ITableService
{
    private readonly HttpClient _httpClient;

    public TableService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        ;
    }

    public async Task<CreateTableResponse?> Create(CreateTableRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/Table", request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CreateTableResponse>();
            }
            else
            {
                // Handle error response here (e.g., log, throw custom exception, etc.)
                return new CreateTableResponse
                {
                    Successful = false,
                    Errors = new[] { "Error creating table" }
                };
            }
        }
        catch (Exception ex)
        {
            // Log the exception or handle it as needed
            return new CreateTableResponse
            {
                Successful = false,
                Errors = new[] { "Unexpected error occurred. Try again or contact support" }
            };
        }
    }

    public async Task<GetTablesResult?> GetList()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/Table");
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<GetTablesResult>(content);
            return result;
        }
        catch (Exception ex)
        {
            // Log the exception or handle it as needed
            throw;
            return new GetTablesResult
            {
                Successful = false,
                Error = $"Error fetching tables: {ex}"
            };
        }
    }


    public async Task<PokerTable?> GetById(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<PokerTable>($"api/Table/{id}");
        }
        catch (Exception ex)
        {
            // Log the exception or handle it as needed
            return null; // Return appropriate response for not found
        }
    }

    public async Task<DeleteTableResult?> Delete(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/Table/{id}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<DeleteTableResult>();
            }
            // Handle error response here
            return new DeleteTableResult
            {
                Successful = false,
                Error = "Error deleting table"
            };
        }
        catch (Exception ex)
        {
            // Log the exception or handle it as needed
            return new DeleteTableResult
            {
                Successful = false,
                Error = "Unexpected error occurred. Try again or contact support"
            };
        }
    }
}