using JokersJunction.GameManagement.Features;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();


builder.Services.AddMassTransit(busConfigurator =>
{
    busConfigurator.SetKebabCaseEndpointNameFormatter();
    busConfigurator.AddConsumer<DisconnectingUserFromGameEventConsumer>();
    busConfigurator.AddConsumer<PlayerStateRefreshEventConsumer>();

    busConfigurator.UsingRabbitMq((context, configurator) =>
    {
        configurator.Host(new Uri(builder.Configuration["MessageBroker:Host"]!), h =>
        {
            h.Username(builder.Configuration["MessageBroker:Username"]);
            h.Password(builder.Configuration["MessageBroker:Password"]);
        });

        configurator.ReceiveEndpoint("disconnecting_user_from_game_queue", e =>
        {
            e.ConfigureConsumer<DisconnectingUserFromGameEventConsumer>(context);
        });

        configurator.ReceiveEndpoint("player_state_refresh_queue", e =>
        {
            e.ConfigureConsumer<PlayerStateRefreshEventConsumer>(context);
        });

        configurator.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
