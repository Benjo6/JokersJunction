using JokersJunction.Bank.Features;
using JokersJunction.Bank.Protos;
using JokersJunction.Bank.Services;
using JokersJunction.Common.Authentication;
using JokersJunction.Shared.Data;
using JokersJunction.Shared.Models;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DBConnection")));


builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddGrpcClient<Currency.CurrencyClient>(options =>
{
    options.Address = new Uri("https://localhost:7158");
});

builder.Services.AddMassTransit(busConfigurator =>
{
    busConfigurator.SetKebabCaseEndpointNameFormatter();
    busConfigurator.AddConsumer<UserDepositEventConsumer>();
    busConfigurator.AddConsumer<UserWithdrawEventConsumer>();

    busConfigurator.UsingRabbitMq((context, configurator) =>
    {
        configurator.Host(new Uri(builder.Configuration["MessageBroker:Host"]!), h =>
        {
            h.Username(builder.Configuration["MessageBroker:Username"]);
            h.Password(builder.Configuration["MessageBroker:Password"]);
        });

        configurator.ReceiveEndpoint("user_deposit_event_queue", e =>
        {
            e.ConfigureConsumer<UserDepositEventConsumer>(context);
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
app.UseAuthentication();
app.UseAuthorization();

// Map gRPC service after the authentication and authorization middleware
app.MapGrpcService<CurrencyService>();

app.MapControllers();

app.Run();