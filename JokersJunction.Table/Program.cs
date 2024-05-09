using JokersJunction.Common.Databases;
using JokersJunction.Common.Databases.Interfaces;
using JokersJunction.Table.Repositories;
using JokersJunction.Table.Repositories.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddRouting(options => options.LowercaseUrls = true);
//builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddScoped<IDatabaseService>(sp => new DatabaseService(builder.Configuration.GetConnectionString("MongoConnection")));

builder.Services.AddScoped<ITableRepository, TableRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
