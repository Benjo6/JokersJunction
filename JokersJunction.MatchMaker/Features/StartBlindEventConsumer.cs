using JokersJunction.Common.Databases.Interfaces;
using JokersJunction.Common.Databases.Models;
using JokersJunction.Common.Events;
using JokersJunction.Common.Events.Responses;
using MassTransit;

namespace JokersJunction.GameUser.Features;

public class StartBlindEventConsumer : IConsumer<StartBlindEvent>
{
    private readonly IDatabaseService _databaseService;
    public StartBlindEventConsumer(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task Consume(ConsumeContext<StartBlindEvent> context)
    {
        var smallBlindUser = await _databaseService.GetOneByNameAsync<User>(context.Message.SmallBlindName);

        if (smallBlindUser.Balance >= context.Message.SmallBlind)
        {
            smallBlindUser.Balance -= context.Message.SmallBlind;
            context.Message.Players.First(e => e.Name == smallBlindUser.Name).RoundBet += context.Message.SmallBlind;
        }
        else
        {
            context.Message.Players.First(e => e.Name == smallBlindUser.Name).RoundBet += smallBlindUser.Balance;
            smallBlindUser.Balance = 0;
        }
        await _databaseService.ReplaceOneAsync(smallBlindUser);

        // Big blind
        var bigBlindUser = await _databaseService.GetOneByNameAsync<User>(context.Message.BigBlindName);
        if (bigBlindUser.Balance >= context.Message.BigBlind)
        {
            bigBlindUser.Balance -= context.Message.BigBlind;
            context.Message.Players.First(e => e.Name == bigBlindUser.Name).RoundBet += context.Message.BigBlind;
        }
        else
        {
            context.Message.Players.First(e => e.Name == bigBlindUser.Name).RoundBet += bigBlindUser.Balance;
            bigBlindUser.Balance = 0;
        }
        await _databaseService.ReplaceOneAsync(bigBlindUser);

        await context.RespondAsync(new StartBlindEventResponse()
        {
            SmallRoundBet = context.Message.Players.First(e => e.Name == smallBlindUser.Name).RoundBet,
            BigRoundBet = context.Message.Players.First(e => e.Name == bigBlindUser.Name).RoundBet
        });
    }
}