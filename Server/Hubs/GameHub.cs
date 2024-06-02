using System.Text;
using System.Text.Json;
using JokersJunction.Common.Databases.Interfaces;
using JokersJunction.Common.Databases.Models;
using JokersJunction.Shared;
using JokersJunction.Shared.Models;
using JokersJunction.Shared.Responses;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace JokersJunction.Server.Hubs;

public class GameHub : Hub
{
    private readonly IDatabaseService _database;
    private readonly HttpClient _httpClient;

    public GameHub(IDatabaseService database, HttpClient httpClient)
    {
        _database = database;
        _httpClient = httpClient;
    }

    public async Task SendMessage(string message)
    {
        var newMessage = new GetMessageResult { Sender = Context.User.Identity.Name, Message = message };
        var user = await _database.GetOneByNameAsync<User>(Context.User.Identity.Name);

        await Clients.Groups(user.TableId).SendAsync("ReceiveMessage", newMessage);
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var userName = Context.User.Identity.Name;
        await DisconnectPlayer(userName);
        await base.OnDisconnectedAsync(exception);
    }

    private async Task DisconnectPlayer(string userName)
    {
        var requestUrl = $"gateway/game-user/disconnect-user/{userName}";
        var requestContent = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(requestUrl, requestContent);

        Console.WriteLine(response.IsSuccessStatusCode
            ? $"Successfully disconnected user: {userName}"
            : $"Failed to disconnect user: {userName}. Status code: {response.StatusCode}");
    }

    public async Task AddToUsers(string tableId, TableType tableType)
    {
        var content = new StringContent(JsonConvert.SerializeObject(new { connectionId = Context.ConnectionId, username = Context.User.Identity.Name, tableId, tableType }), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("gateway/game-user/add-users/" + tableId, content);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadAsStringAsync();
            var addUsersResponse = JsonConvert.DeserializeObject<AddUsersResponse>(result);

            switch (addUsersResponse)
            {
                case AddUsersResponse.Done:
                    await Groups.AddToGroupAsync(Context.ConnectionId, tableId);
                    break;
                case AddUsersResponse.ReceiveKick:
                    await Clients.Client(Context.ConnectionId).SendAsync("ReceiveKick");
                    break;
            }
        }
    }

    public async Task MarkReady(int depositAmount)
    {
        var content = new StringContent(JsonConvert.SerializeObject(new { username = Context.User.Identity.Name, depositAmount }), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("gateway/game/mark-ready/" + Context.User.Identity.Name, content);
        if (response.IsSuccessStatusCode)
        {
            return;
        }
        else
        {
            throw new Exception("We do not know what is going on");
        }
    }

    public async Task UnmarkReady()
    {
        var content = new StringContent(JsonConvert.SerializeObject(new { username = Context.User.Identity.Name }), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("gateway/game/unmark-ready/" + Context.User.Identity.Name, content);
        if (response.IsSuccessStatusCode)
        {
            return;
        }
        else
        {
            throw new Exception("We do not know what is going on");
        }
    }

    public async Task ActionFold(string username)
    {
        var url = $"gateway/game/action-fold/{username}";
        await APIPostCall(url);
    }

    public async Task ActionCheck(string username)
    {
        var url = $"gateway/game/action-check/{username}";
        await APIPostCall(url);
    }

    public async Task ActionRaise(string username, int raiseAmount)
    {
        var url = $"gateway/game/action-raise/{username}";
        var body = JsonSerializer.Serialize(new { raiseAmount }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await APIPostCall(url, body);
    }

    public async Task ActionCall(string username)
    {
        var url = $"gateway/game/action-call/{username}";
        await APIPostCall(url);
    }

    public async Task ActionAllIn(string username)
    {
        var url = $"gateway/game/action-all-in/{username}";
        await APIPostCall(url);
    }

    public async Task StartBlackjackGame(string tableId)
    {
        var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"gateway/game/start-blackjack-game/{tableId}", content);
        if (response.IsSuccessStatusCode)
        {
            return;
        }
        else
        {
            throw new Exception("We do not know what is going on");
        }
    }

    public async Task BlackjackHit(string playerName)
    {
        var url = $"gateway/game/blackjack-game-hit/{playerName}";
        await APIPostCall(url);
    }

    public async Task BlackjackStand(string playerName)
    {
        var url = $"gateway/game/blackjack-game-stand/{playerName}";
        await APIPostCall(url);
    }

    public async Task BlackjackBet(string playerName, int betAmount)
    {
        var url = $"gateway/game/blackjack-bet/{playerName}";
        var body = JsonSerializer.Serialize(new { betAmount }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await APIPostCall(url, body);
    }

    private async Task APIPostCall(string url, string body = "")
    {
        var requestContent = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(url, requestContent);

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"{url} call succeeded.");
        }
        else
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"{url} call failed. Status Code: {response.StatusCode}, Response: {responseBody}");
        }
    }
}