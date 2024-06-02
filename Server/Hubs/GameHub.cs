using System.Collections;
using System.Text;
using JokersJunction.Common.Databases.Interfaces;
using JokersJunction.Common.Databases.Models;
using JokersJunction.Common.Events.Responses;
using JokersJunction.Server.Evaluators;
using JokersJunction.Server.Responses;
using JokersJunction.Shared;
using JokersJunction.Shared.Models;
using JokersJunction.Shared.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using PokerTable = JokersJunction.Common.Databases.Models.PokerTable;

namespace JokersJunction.Server.Hubs;

public class GameHub : Hub
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IDatabaseService _database;
    private readonly HttpClient _httpClient;

    public GameHub(UserManager<ApplicationUser> userManager, IDatabaseService database, HttpClient httpClient)
    {
        _userManager = userManager;
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
    }

    public async Task AddToUsers(string tableId)
    {
        var content = new StringContent(JsonConvert.SerializeObject(new { connectionId = Context.ConnectionId, username = Context.User.Identity.Name, tableId = tableId }), Encoding.UTF8, "application/json");
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


    public async Task ActionFold()
    {
    }

    public async Task ActionCheck()
    {
    }

    public async Task ActionRaise(int raiseAmount)
    {
       
    }

    public async Task ActionCall()
    {
    }

    public async Task ActionAllIn()
    {
    }

}