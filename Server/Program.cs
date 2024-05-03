using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using JokersJunction.Server.Data;
using JokersJunction.Server.Hubs;
using JokersJunction.Shared.Models;
using Ocelot.DependencyInjection;
using JokersJunction.Authentication.Protos;
using JokersJunction.Bank.Protos;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DBConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 10;
    options.Password.RequiredUniqueChars = 3;
    options.Password.RequireNonAlphanumeric = false;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtIssuer"],
            ValidAudience = builder.Configuration["JwtAudience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSecurityKey"]))
        };
    });

builder.Services.AddSignalR();
builder.Services.AddGrpcClient<Authorizer.AuthorizerClient>(options =>
{
    options.Address = new Uri("https://localhost:5001");
});
builder.Services.AddGrpcClient<Currency.CurrencyClient>(options =>
{
    options.Address = new Uri("https://localhost:5002");
});

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddControllersWithViews();

// Add Ocelot services
builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseRouting();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();


// Add this middleware to validate the token and set HttpContext.User
app.Use(async (context, next) =>
{
    var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
    if (token != null)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(builder.Configuration["JwtSecurityKey"]!);
        var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,  // Set to true
            ValidateAudience = true,  // Set to true
            ValidIssuer = builder.Configuration["JwtIssuer"],  // Set to the correct value
            ValidAudience = builder.Configuration["JwtAudience"],  // Set to the correct value
            ClockSkew = TimeSpan.Zero
        }, out var validatedToken);

        context.User = principal;
    }

    await next();
});

// Use Ocelot
//await app.UseOcelot();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();


app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHub<GameHub>("/gameHub");
});

app.Run();
