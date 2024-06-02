using JokersJunction.Common.Databases;
using JokersJunction.Common.Databases.Interfaces;
using JokersJunction.Table.Features;
using JokersJunction.Table.Repositories;
using JokersJunction.Table.Repositories.Interfaces;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddScoped<IDatabaseService>(sp => new DatabaseService(builder.Configuration.GetConnectionString("MongoConnection")));

builder.Services.AddScoped<ITableRepository, TableRepository>();

builder.Services.AddMassTransit(busConfigurator =>
{
    busConfigurator.SetKebabCaseEndpointNameFormatter();
    busConfigurator.AddConsumer<CurrentPokerTableEventConsumer>();
    busConfigurator.AddConsumer<CurrentBlackjackTableEventConsumer>();
    busConfigurator.UsingRabbitMq((context, configurator) =>
    {
        configurator.Host(new Uri(builder.Configuration["MessageBroker:Host"]!), h =>
        {
            h.Username(builder.Configuration["MessageBroker:Username"]);
            h.Password(builder.Configuration["MessageBroker:Password"]);
        });

        configurator.ReceiveEndpoint("current_poker_table_queue", e =>
        {
            e.ConfigureConsumer<CurrentPokerTableEventConsumer>(context);
        });
        configurator.ReceiveEndpoint("current_blackjack_table_queue", e =>
        {
            e.ConfigureConsumer<CurrentBlackjackTableEventConsumer>(context);
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
