using JokersJunction.Common.Databases.Interfaces;
using JokersJunction.Common.Databases.Models;
using JokersJunction.Common.Events;
using JokersJunction.Common.Events.Responses;
using JokersJunction.GameManagement.Services.Contracts;
using JokersJunction.Shared;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace JokersJunction.GameManagement.Controllers;

[Route("api/[controller]")]
[ApiController]
public class GameController : ControllerBase
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IDatabaseService _databaseService;
    private readonly IGameService _gameService;
    private readonly IRequestClient<GetUsersEvent> _getUsersRequestClient;
    private readonly IRequestClient<UserIsReadyEvent> _userIsReadyRequestClient;
    private readonly IRequestClient<GetUserByNameEvent> _getUserByNameRequestClient;
    public GameController(IPublishEndpoint publishEndpoint, IDatabaseService databaseService, IGameService gameService, IRequestClient<GetUsersEvent> getUsersRequestClient, IRequestClient<UserIsReadyEvent> userIsReadyRequestClient, IRequestClient<GetUserByNameEvent> getUserByNameRequestClient)
    {
        _publishEndpoint = publishEndpoint;
        _databaseService = databaseService;
        _gameService = gameService;
        _getUsersRequestClient = getUsersRequestClient;
        _userIsReadyRequestClient = userIsReadyRequestClient;
        _getUserByNameRequestClient = getUserByNameRequestClient;
    }

    [HttpPost("mark-ready/{username}")]
    public async Task<IActionResult> MarkReady(int depositAmount, string username)
    {
        var responseUser = await _userIsReadyRequestClient.GetResponse<UserIsReadyEventResponse>(new UserIsReadyEvent()
        { 
            Name = username,
        });
        var user = responseUser.Message.User;
        if (user is null)
        {
            return Empty;
        }
        await _publishEndpoint.Publish(new UserDepositEvent
        {
            User = user,
            Amount = depositAmount
        }); ;

        var games = await _databaseService.ReadAsync<Game>();
        await _gameService.PlayerStateRefresh(user.TableId, games);
        var responseUsers = await _getUsersRequestClient.GetResponse<GetUsersEventResponse>(new GetUsersEvent());
        var users = responseUsers.Message.Users;
        games = await _databaseService.ReadAsync<Game>();
        if (users.Where(e => e.TableId == user.TableId).Count(e => e.IsReady) >= 2 &&
            games.All(e => e.TableId != user.TableId))
        {
            await _gameService.StartGame(user.TableId, 0, users);
        }
        return Ok();
    }

    [HttpPost("unmark-ready/{username}")]
    public async Task<IActionResult> UnmarkReady(string username)
    {
        var responseUser = await _userIsReadyRequestClient.GetResponse<UserIsReadyEventResponse>(new UserIsReadyEvent()
        {
            Name = username,
        });
        var user = responseUser.Message.User;
        await _publishEndpoint.Publish(new UserWithdrawEvent
        {
            GameUser = user
        });
        var games = await _databaseService.ReadAsync<Game>();

        await _gameService.PlayerStateRefresh(user.TableId, games);
        return Ok();
    }

    [HttpPost("action-check/{username}")]
    public async Task<IActionResult> ActionCheck(string username)
    {
        var responseUser = await _getUserByNameRequestClient.GetResponse<GetUserByNameEventResponse>(new GetUserByNameEvent()
        {
            Name = username,
        });
        var user = responseUser.Message.GameUser;
        var tableId = user.TableId;
        var games = await _databaseService.ReadAsync<Game>();
        var currentGame = games.First(x => x.TableId == tableId);
        if (currentGame.Players.Any() && currentGame.GetPlayerNameByIndex(currentGame.Index) == username)
        {
            await _gameService.MoveIndex(tableId, currentGame);
        }
        return Ok();
    }

    [HttpPost("action-fold/{username}")]
    public async Task ActionFold(string username)
    {
        var responseUser = await _getUserByNameRequestClient.GetResponse<GetUserByNameEventResponse>(new GetUserByNameEvent()
        {
            Name = username
        });
        var user = responseUser.Message.GameUser;
        var tableId = user.TableId;
        var games = await _databaseService.ReadAsync<Game>();
        var currentGame = games.First(x => x.TableId == tableId);
        if (currentGame.Players.Any() && currentGame.GetPlayerNameByIndex(currentGame.Index) == username)
        {
            //PlayerFolded
            currentGame.Players.First(e => e.Name == username).ActionState = PlayerActionState.Folded;

            //Remove from pots
            foreach (var pot in currentGame.Winnings)
            {
                pot.Players.Remove(username);
            }
            await _databaseService.ReplaceOneAsync(currentGame);

            //CheckIfOnlyOneLeft
            games = await _databaseService.ReadAsync<Game>();
            currentGame = games.First(x => x.TableId == tableId);
            if (currentGame.Players.Count(e => e.ActionState == PlayerActionState.Playing) == 1)
            {
                await _gameService.UpdatePot(currentGame);
                await _gameService.GetAndAwardWinners(currentGame);
                await _gameService.PlayerStateRefresh(tableId, games);

                Thread.Sleep(10000);

                await _databaseService.DeleteOneAsync(currentGame);
                var responseUsers = await _getUsersRequestClient.GetResponse<GetUsersEventResponse>(new GetUsersEvent());
                var users = responseUsers.Message.Users;
                if (users.Where(e => e.TableId == tableId).Count(e => e.IsReady) >= 2 && !users.Any(e => e.TableId == tableId))
                {
                    await _gameService.StartGame(tableId, currentGame.SmallBlindIndex + 1, users);
                }
                else
                {
                    foreach (var e in users.Where(e => e.TableId == tableId))
                    {
                        e.InGame = false;
                        await _databaseService.ReplaceOneAsync(e);
                    }
                    await _gameService.PlayerStateRefresh(tableId,games);
                }
            }
            else
            {
                await _gameService.MoveIndex(tableId, currentGame);
            }
        }
    }

    [HttpPost("action-raise/{username}")]
    public async Task ActionRaise(string username, int raiseAmount)
    {
        var responseUser = await _getUserByNameRequestClient.GetResponse<GetUserByNameEventResponse>(new GetUserByNameEvent()
        {
            Name = username
        });
        var user = responseUser.Message.GameUser;
        var tableId = user.TableId;
        var games = await _databaseService.ReadAsync<Game>();
        var currentGame = games.First(e => e.TableId == tableId);
        var currentPlayer = currentGame.Players.First(e => e.Name == username);

        if (currentGame.Players.Any() && currentGame.GetPlayerNameByIndex(currentGame.Index) == username &&
            user.Balance > raiseAmount + currentGame.RaiseAmount - currentPlayer.RoundBet)
        {
            await _publishEndpoint.Publish(new UserRaiseEvent
            {
                GameUser = user,
                Amount = raiseAmount + currentGame.RaiseAmount - currentPlayer.RoundBet
            });


            currentPlayer.RoundBet = raiseAmount + currentGame.RaiseAmount;
            currentGame.RaiseAmount += raiseAmount;
            currentGame.RoundEndIndex = currentGame.Index;
            await _databaseService.ReplaceOneAsync(currentGame);

            await _gameService.MoveIndex(tableId, currentGame);
        }
    }

    [HttpPost("action-call/{username}")]
    public async Task ActionCall(string username)
    {
        var responseUser = await _getUserByNameRequestClient.GetResponse<GetUserByNameEventResponse>(new GetUserByNameEvent()
        {
            Name = username
        });
        var user = responseUser.Message.GameUser;
        var tableId = user.TableId;

        var games = await _databaseService.ReadAsync<Game>();
        var currentGame = games.First(e => e.TableId == tableId);
        var currentPlayer = currentGame.Players.First(e => e.Name == username);

        if (currentGame.Players.Any() && currentGame.GetPlayerNameByIndex(currentGame.Index) == username && currentGame.RaiseAmount > 0)
        {
            if (currentGame.RaiseAmount - currentPlayer.RoundBet < user.Balance)
            {
                user.Balance -= currentGame.RaiseAmount - currentPlayer.RoundBet;
                currentPlayer.RoundBet = currentGame.RaiseAmount;
            }
            else if (currentGame.RaiseAmount - currentPlayer.RoundBet >= user.Balance)
            {
                var allInSum = user.Balance;
                user.Balance = 0;
                currentPlayer.RoundBet = allInSum;
            }
            await _databaseService.ReplaceOneAsync(currentGame);
            await _publishEndpoint.Publish(new UpdateUserEvent()
            {
                GameUser = user
            });

            await _gameService.MoveIndex(tableId, currentGame);
        }
    }

    [HttpPost("action-all-in/{username}")]
    public async Task ActionAllIn(string username)
    {
        var responseUser = await _getUserByNameRequestClient.GetResponse<GetUserByNameEventResponse>(new GetUserByNameEvent()
        {
            Name = username
        });
        var user = responseUser.Message.GameUser;
        var tableId = user.TableId;

        var games = await _databaseService.ReadAsync<Game>();
        var currentGame = games.First(e => e.TableId == tableId);
        var currentPlayer = currentGame.Players.First(e => e.Name == username);

        if (currentGame.Players.Any() && currentGame.GetPlayerNameByIndex(currentGame.Index) == username &&
            user.Balance > currentGame.RaiseAmount - currentPlayer.RoundBet)
        {
            var allInSum = user.Balance;
            user.Balance = 0;
            currentPlayer.RoundBet = allInSum;
            currentGame.RaiseAmount += allInSum;
            currentGame.RoundEndIndex = currentGame.Index;
            await _databaseService.ReplaceOneAsync(currentGame);
            await _publishEndpoint.Publish(new UpdateUserEvent()
            {
                GameUser = user
            });

            await _gameService.MoveIndex(tableId, currentGame);
        }
    }
}