﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using My_Api.Models;
using My_Api.Services;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860


namespace My_Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class UserController : Controller
    {

        private readonly AlexwilkinsonContext _context;
        private readonly IConfiguration _configuration;
        private IUserService _userService;


        public UserController(AlexwilkinsonContext context, IConfiguration configuration, IUserService userService)
        {
            _context = context;
            _configuration = configuration;
            _userService = userService;
        }
        [HttpGet]
        public IActionResult GetUserDetails([FromHeader] string authorization)
        {
            var jwtToken = authorization.Split(" ")[1];

            var user = _userService.DecodeTokenUser(jwtToken);

            return Ok(user);
        }

        [HttpGet("users")]
        public IActionResult GetUsers()
        {
            var users = _context.User
                .Where(u => u.IsActive == true)
                .ToList();

            return Ok(_userService.RemoveSensitiveData(users));
        }

        [AllowAnonymous]
        [HttpPost("Authenticate")]
        public IActionResult Authenticate([FromBody] AuthenticateModel model)
        {

            var user = _userService.Authenticate(model.Email, model.Password);

            if(user == null)
            {
                return BadRequest(new
                {
                    message = "Username or password incorrect"
                });
            }



            return Ok(user);
        }

        [AllowAnonymous]
        [HttpPost("Register")]
        public IActionResult Register([FromBody]  RegisterModel user)
        {
            var addedUser = _userService.Register(user);


            return Ok(addedUser);
        }

    }
}
