using JokersJunction.Common.Databases.Interfaces;
using JokersJunction.Common.Databases.Models;
using JokersJunction.Common.Events;
using JokersJunction.Common.Events.Responses;
using JokersJunction.Server.Events;
using JokersJunction.Server.Events.Response;
using JokersJunction.Shared.Responses;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace JokersJunction.MatchMaker.Controllers;

[Route("api/game-user")]
[ApiController]
public class GameUserController : ControllerBase
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IDatabaseService _databaseService;
    private readonly IRequestClient<CurrentTableEvent> _currentTableRequestClient;
    private IRequestClient<DisconnectingUserFromGameEvent> _disconnectingUserFromGameRequestClient;

    public GameUserController(IPublishEndpoint publishEndpoint, IDatabaseService databaseService, IRequestClient<CurrentTableEvent> currentTableRequestClient, IRequestClient<DisconnectingUserFromGameEvent> disconnectingUserFromGameRequestClient)
    {
        _publishEndpoint = publishEndpoint;
        _databaseService = databaseService;
        _currentTableRequestClient = currentTableRequestClient;
        _disconnectingUserFromGameRequestClient = disconnectingUserFromGameRequestClient;
    }
    [HttpPost("add-users/{connectionId}")]
    public async Task<IActionResult> AddToUsers(string connectionId, string username, string tableId)
    {
        var response = await _currentTableRequestClient.GetResponse<CurrentTableEventResponse>(new CurrentTableEvent()
        {
            TableId = tableId
        });
        var users = await _databaseService.ReadAsync<User>();
        if (users.Count(e => e.TableId == tableId) >= response.Message.Table.MaxPlayers)
        {
            return Ok(AddUsersResponse.ReceiveKick);
        }

        if (users.Any(e => e.Name == username))
        {
            return Ok(AddUsersResponse.ReceiveKick);
        }
        var seatNumber = AssignTableToUser(tableId, users);
        var newUser = new User
        {
            Name = username,
            TableId = tableId,
            ConnectionId = connectionId,
            SeatNumber = seatNumber,
            InGame = false,
            Balance = 0
        };
        _databaseService.InsertOne(newUser);
        await _publishEndpoint.Publish(new PlayerStateRefreshEvent
        {
            TableId = tableId,
        });

        return Ok(AddUsersResponse.Done);
    }

    [HttpPost("disconnect-user/{username}")]
    public async Task<IActionResult> DisconnectUser(string username)
    {
        try
        {
            var user = await _databaseService.GetOneByNameAsync<User>(username);
            var tableId = user.TableId;
            var smallBlindIndexTemp = 0;
            
            if (user.InGame)
            {
                var response = await _disconnectingUserFromGameRequestClient.GetResponse<DisconnectingUserFromGameEventResponse>(new DisconnectingUserFromGameEvent()
                {
                    TableId = tableId,
                    UserName = user.Name
                });
               smallBlindIndexTemp = response.Message.SmallBlindIndex;
            }

            await _publishEndpoint.Publish(new UserWithdrawEvent()
            {
                GameUser = user
            });
            user = await _databaseService.GetOneByNameAsync<User>(user.Name);
            await _databaseService.DeleteOneAsync(user);
            var users = await _databaseService.ReadAsync<User>();
            foreach (var e in users.Where(e => e.TableId == tableId))
            {
                e.InGame = false;
                await _databaseService.ReplaceOneAsync(e);
            }
            users = await _databaseService.ReadAsync<User>();
            if (users.Count(e => e.IsReady) >= 2 && users.All(e => e.TableId != tableId))
            {
                await _publishEndpoint.Publish(new StartGameEvent
                {
                    TableId = tableId,
                    SmallBlindPosition = smallBlindIndexTemp + 1
                });
            }
            else
            {
                await _publishEndpoint.Publish(new PlayerStateRefreshEvent
                {
                    TableId = tableId,
                });
            }
        }
        catch (Exception)
        {
            //Log
        }

        return Ok();
    }

    private int AssignTableToUser(string tableId, List<User> users)
    {
        var occupiedSeats = users.Where(e => e.TableId == tableId).Select(e => e.SeatNumber).OrderBy(e => e).ToList();
        for (var i = 0; i < occupiedSeats.Count; i++)
        {
            if (occupiedSeats[i] != i) return i;
        }
        return occupiedSeats.Count;
    }
}