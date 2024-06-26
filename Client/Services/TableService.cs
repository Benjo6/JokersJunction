﻿using JokersJunction.Shared;
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
        public async Task<CreateTableResult> CreatePoker(CreateTableModel model)
        {
            var response = await _httpClient.PostAsJsonAsync("api/p-table", model);

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
        public async Task<CreateTableResult> CreateBlackjack(CreateTableModel model)
        {
            var response = await _httpClient.PostAsJsonAsync("api/b-table", model);

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

        public async Task<GetBlackjackTablesResult> GetBlackjackList()
        {
            var result = await _httpClient.GetFromJsonAsync<GetBlackjackTablesResult>("api/b-table");
            return result;
        }

        public async Task<GetPokerTablesResult> GetPokerList()
        {
            var result = await _httpClient.GetFromJsonAsync<GetPokerTablesResult>("api/p-table");
            return result;
        }

        public async Task<PokerTable> GetByPokerId(int id)
        {
            var result = await _httpClient.GetFromJsonAsync<PokerTable>($"api/p-table/{id}");
            return result;
        }

        public async Task<BlackjackTable> GetByBlackjackId(int id)
        {
            var result = await _httpClient.GetFromJsonAsync<BlackjackTable>($"api/b-table/{id}");
            return result;
        }

        public async Task<DeleteTableResult> DeletePoker(int id)
        {
            var response = await _httpClient.PostAsJsonAsync($"api/p-table/delete", id);

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

        public async Task<DeleteTableResult> DeleteBlackjack(int id)
        {
            var response = await _httpClient.PostAsJsonAsync($"api/b-table/delete", id);

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
