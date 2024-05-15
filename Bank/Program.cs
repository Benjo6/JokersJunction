using JokersJunction.Bank.Protos;
using JokersJunction.Bank.Services;
using JokersJunction.Shared.Data;
using JokersJunction.Shared.Models;
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

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseAuthentication();
app.UseAuthorization();

// Map gRPC service after the authentication and authorization middleware
app.MapGrpcService<CurrencyService>();

app.MapControllers();

app.Run();