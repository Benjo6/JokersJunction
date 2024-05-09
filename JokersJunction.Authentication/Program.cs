using JokersJunction.Authentication.Protos;
using JokersJunction.Authentication.Services;
using JokersJunction.Shared.Data;
using JokersJunction.Shared.Models;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DBConnection")));

builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 10;
        options.Password.RequiredUniqueChars = 3;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddGrpcClient<Authorizer.AuthorizerClient>(options =>
{
    options.Address = new Uri("https://localhost:7017");
});

// Add MassTransit
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<AuthenticationService>();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();