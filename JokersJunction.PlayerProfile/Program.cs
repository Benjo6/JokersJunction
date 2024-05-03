using JokersJunction.PlayerProfile.Repositories;
using JokersJunction.PlayerProfile.Repositories.Interfaces;
using JokersJunction.Server.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DBConnection")));
builder.Services.AddControllers();
builder.Services.AddScoped<IPlayerNotesRepository, PlayerNotesRepository>();


var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();
app.UseAuthentication();

app.MapControllers();

app.Run();
