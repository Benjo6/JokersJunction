using JokersJunction.PlayerProfile.Repositories;
using JokersJunction.PlayerProfile.Repositories.Interfaces;
using JokersJunction.Server.Data;
using JokersJunction.Shared.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddRouting(options => options.LowercaseUrls = true); 
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DBConnection")));

builder.Services.AddScoped<IPlayerNotesRepository, PlayerNotesRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
