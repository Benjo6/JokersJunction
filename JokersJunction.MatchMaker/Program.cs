using JokersJunction.Common.Databases;
using JokersJunction.Common.Databases.Interfaces;
using JokersJunction.GameUser.Features;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddScoped<IDatabaseService>(sp => new DatabaseService(builder.Configuration.GetConnectionString("MongoConnection")));

builder.Services.AddMassTransit(busConfigurator =>
{
    busConfigurator.SetKebabCaseEndpointNameFormatter();
    busConfigurator.AddConsumer<GetUsersEventConsumer>();
    busConfigurator.AddConsumer<StartBlindEventConsumer>();
    busConfigurator.AddConsumer<UpdateUserEventConsumer>();
    busConfigurator.AddConsumer<UserDepositEventConsumer>();
    busConfigurator.AddConsumer<UserInGameEventConsumer>();
    busConfigurator.AddConsumer<UserIsReadyEventConsumer>();
    busConfigurator.AddConsumer<UserRaiseEventConsumer>();
    busConfigurator.AddConsumer<UserWithdrawEventConsumer>();

    busConfigurator.UsingRabbitMq((context, configurator) =>
    {
        configurator.Host(new Uri(builder.Configuration["MessageBroker:Host"]!), h =>
        {
            h.Username(builder.Configuration["MessageBroker:Username"]!);
            h.Password(builder.Configuration["MessageBroker:Password"]!);
        });

        configurator.ReceiveEndpoint("get_users_event_queue", e =>
        {
            e.ConfigureConsumer<GetUsersEventConsumer>(context);
        });

        configurator.ReceiveEndpoint("start_blind_event_queue", e =>
        {
            e.ConfigureConsumer<StartBlindEventConsumer>(context);
        });

        configurator.ReceiveEndpoint("update_user_event_queue", e =>
        {
            e.ConfigureConsumer<UpdateUserEventConsumer>(context);
        });

        configurator.ReceiveEndpoint("user_deposit_event_queue", e =>
        {
            e.ConfigureConsumer<UserDepositEventConsumer>(context);
        });

        configurator.ReceiveEndpoint("user_in_game_event_queue", e =>
        {
            e.ConfigureConsumer<UserInGameEventConsumer>(context);
        });

        configurator.ReceiveEndpoint("user_is_ready_event_queue", e =>
        {
            e.ConfigureConsumer<UserIsReadyEventConsumer>(context);
        });

        configurator.ReceiveEndpoint("user_raise_event_queue", e =>
        {
            e.ConfigureConsumer<UserRaiseEventConsumer>(context);
        });

        configurator.ReceiveEndpoint("user_withdraw_event_queue", e =>
        {
            e.ConfigureConsumer<UserWithdrawEventConsumer>(context);
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
