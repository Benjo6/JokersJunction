using JokersJunction.Shared.Events;
using MassTransit;

namespace JokersJunction.Bank.Features;

public sealed class UserDisconnectedConsumer : IConsumer<UserDisconnectedEvent>
{
    public Task Consume(ConsumeContext<UserDisconnectedEvent> context)
    {
        throw new NotImplementedException();
    }
}