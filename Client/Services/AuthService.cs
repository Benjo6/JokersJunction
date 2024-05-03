using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Blazored.LocalStorage;
using JokersJunction.Shared.Requests;
using JokersJunction.Shared.Responses;
using Microsoft.AspNetCore.Components.Authorization;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace JokersJunction.Client.Services
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private readonly ILocalStorageService _localStorage;

        public AuthService(HttpClient httpClient,
                           AuthenticationStateProvider authenticationStateProvider,
                           ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _authenticationStateProvider = authenticationStateProvider;
            _localStorage = localStorage;
        }

        public async Task<RegisterResponse> Register(RegisterRequest registerModel)
        {
            var response = await _httpClient.PostAsJsonAsync("auth/register", registerModel);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();
                return result;
            }
            else
            {
                // Handle error response here
                return null;
            }
        }

        public async Task<int> GetBalance()
        {
            try
            {
                var userName = await GetCurrentUserName();
                var response = await _httpClient.GetAsync($"currency/balance?userName={userName}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<int>();
                    return result;
                }
                else
                {
                    // Handle error response here
                    return -1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting balance: {ex.Message}");
                return -1;
            }
        }



        public async Task<LoginResponse> Login(LoginRequest request)
        {
            var loginAsJson = JsonSerializer.Serialize(request);
            var response = await _httpClient.PostAsync("auth/login", new StringContent(loginAsJson, Encoding.UTF8, "application/json"));
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error: {responseContent}");
                return null;
            }
            else
            {
                try
                {
                    var result = JsonSerializer.Deserialize<LoginResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    await _localStorage.SetItemAsync("authToken", result.Token);
                    await _localStorage.SetItemAsync("currentTable", 0);
                    ((ApiAuthenticationStateProvider)_authenticationStateProvider).MarkUserAsAuthenticated(result.Token);
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", result.Token);
                    return result;
                }
                catch (System.Text.Json.JsonException ex)
                {
                    Console.WriteLine($"Invalid JSON format: {responseContent}");
                    throw;
                }
            }
        }

        public async Task Logout()
        {
            await _localStorage.RemoveItemAsync("authToken");
            await _localStorage.RemoveItemAsync("currentTable");
            ((ApiAuthenticationStateProvider)_authenticationStateProvider).MarkUserAsLoggedOut();
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }


        private async Task<string?> GetCurrentUserName()
        {
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            return user.Identity?.Name;
        }

    }
}
