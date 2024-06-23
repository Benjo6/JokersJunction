﻿using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Grpc.Core;
using JokersJunction.Authentication.Protos;
using JokersJunction.Shared.Events;
using JokersJunction.Shared.Models;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;
using LoginRequest = JokersJunction.Authentication.Protos.LoginRequest;
using RegisterRequest = JokersJunction.Authentication.Protos.RegisterRequest;

namespace JokersJunction.Authentication.Services;

public class AuthenticationService : Authorizer.AuthorizerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IPublishEndpoint _publishEndpoint;
    public AuthenticationService(UserManager<ApplicationUser> userManager, IConfiguration configuration, SignInManager<ApplicationUser> signInManager, IPublishEndpoint publishEndpoint)
    {
        _userManager = userManager;
        _configuration = configuration;
        _signInManager = signInManager;
        _publishEndpoint = publishEndpoint;
    }

    public override async Task<RegisterResponse> Register(RegisterRequest request, ServerCallContext context)
    {
        var newUser = new ApplicationUser { UserName = request.Email, Email = request.Email };

        var result = await _userManager.CreateAsync(newUser, request.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(x => x.Description);

            return new RegisterResponse { Successful = false, Errors = { errors } };
        }

        await _userManager.AddToRoleAsync(newUser, "User");

        return new RegisterResponse { Successful = true };
    }

    public override async Task<LoginResponse> Login(LoginRequest request, ServerCallContext context)
    {
        var result = await _signInManager.PasswordSignInAsync(request.Email, request.Password, false, false);

        if (!result.Succeeded) return new LoginResponse { Successful = false, Error = "Username and password are invalid." };

        var user = await _userManager.FindByEmailAsync(request.Email);
        var roles = await _userManager.GetRolesAsync(user ?? throw new InvalidOperationException());

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, request.Email),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = GenerateJwtToken(claims);

        return new LoginResponse { Successful = true, Token = new JwtSecurityTokenHandler().WriteToken(token) };
    }

    //public override async Task<DisconnectResponse> Disconnect(DisconnectRequest request, ServerCallContext context)
    //{
    //    var user = await _userManager.FindByNameAsync(request.Username);

    //    if (user is null)
    //    {
    //        return new DisconnectResponse { Successful = false, Error = "User not found." };
    //    }

    //    await _publishEndpoint.Publish(new UserDisconnectedEvent { Username = request.Username });

    //    return new DisconnectResponse { Successful = true };
    //}

    private JwtSecurityToken GenerateJwtToken(IEnumerable<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSecurityKey"] ?? throw new InvalidOperationException()));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.Now.AddDays(Convert.ToInt32(_configuration["JwtExpiryInDays"]));

        var token = new JwtSecurityToken(
            _configuration["JwtIssuer"],
            _configuration["JwtAudience"],
            claims,
            expires: expiry,
            signingCredentials: credentials
        );

        return token;
    }
}