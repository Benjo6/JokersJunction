using JokersJunction.Server.Data;
using JokersJunction.Table.Repositories.Interfaces;
using JokersJunction.Table.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddAutoMapper(typeof(Program));


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DBConnection")));


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddScoped<ITableRepository, TableRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseRouting();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();