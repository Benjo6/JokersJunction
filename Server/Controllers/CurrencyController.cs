﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using JokersJunction.Shared;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace JokersJunction.Server.Controllers
{
    [Route("api/currency")]
    [ApiController]
    public class CurrencyController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public CurrencyController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task Add(int amount, string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            user.Currency += amount;
            await _userManager.UpdateAsync(user);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task Remove(int amount, string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user.Currency >= amount)
            {
                user.Currency -= amount;
                await _userManager.UpdateAsync(user);
            }
            //error

        }

        [Authorize(Roles = "User,Admin")]
        [HttpGet]
        public async Task<int> Balance()
        {
            var usernameClaim = HttpContext.User.FindFirst(ClaimTypes.Name);
            if (usernameClaim == null)
            {
            }

            var username = usernameClaim.Value;
            var user = await _userManager.FindByNameAsync(username);
            return user.Currency;
        }


    }
}
