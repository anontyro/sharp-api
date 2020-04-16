using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

public class UserOutput
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string Email { get; set; }
}

namespace My_Api.Controllers
{
    [ApiController]
    [Route("user")]
    public class UserController : Controller
    {

        private readonly AlexwilkinsonContext _context;


        public UserController(AlexwilkinsonContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IEnumerable<UserOutput> GetUsers()
        {
            var users = _context.User
                .Where(u => u.IsActive == true)
                .Select(x => new UserOutput
                {
                    Id = x.Id,
                    FirstName = x.FirstName,
                    Email = x.Email,
                }).ToList();

            return users;
        }

    }
}
