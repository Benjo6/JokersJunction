using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Blazored.LocalStorage;
using JokersJunction.Shared.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Newtonsoft.Json;
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

        public async Task<RegisterResult> Register(RegisterModel registerModel)
        {
            var response = await _httpClient.PostAsJsonAsync("api/accounts", registerModel);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<RegisterResult>();
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
                var response = await _httpClient.GetAsync($"api/currency/balance/{userName}");

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



        public async Task<LoginResult> Login(LoginModel loginModel)
        {
            var loginAsJson = JsonSerializer.Serialize(loginModel);
            var response = await _httpClient.PostAsync("api/Login", new StringContent(loginAsJson, Encoding.UTF8, "application/json"));
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
                    var loginResult = JsonSerializer.Deserialize<LoginResult>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    await _localStorage.SetItemAsync("authToken", loginResult.Token);
                    await _localStorage.SetItemAsync("currentTable", 0);
                    ((ApiAuthenticationStateProvider)_authenticationStateProvider).MarkUserAsAuthenticated(loginResult.Token);
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", loginResult.Token);
                    return loginResult;
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
