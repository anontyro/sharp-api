using System.Linq;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using My_Api.Models;
using My_Api.Services;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860


namespace My_Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {

        private readonly AlexwilkinsonContext _context;
        private IUserService _userService;
        private ITokenService _tokenService;


        public UserController(AlexwilkinsonContext context, IUserService userService, ITokenService tokenService)
        {
            _context = context;
            _userService = userService;
            _tokenService = tokenService;
        }
        [HttpGet]
        public IActionResult GetUserDetails([FromHeader] string authorization)
        {
            var jwtToken = authorization.Split(" ")[1];

            var user = _tokenService.DecodeJwtToken(jwtToken);

            return Ok(user);
        }

        [HttpGet("Users")]
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

        [AllowAnonymous]
        [HttpGet("Activate")]
        public IActionResult Activate([FromQuery] string token)
        {

            if(token == null)
            {
                return BadRequest(new
                {
                    Message = "Token is required for validation"
                });
            }

            var activatedUser = _userService.Activate(HttpUtility.UrlDecode(token));

            if(activatedUser == null)
            {
                return BadRequest(new
                {
                    Message = "No user can be found that needs to be activated for this token"
                });
            }

            return Ok(activatedUser);
        }

        [AllowAnonymous]
        [HttpPost("Recover")]
        public IActionResult RecoverPasswordToken([FromBody] RecoverPasswordModel recoverPassword)
        {

            var token = _userService.RecoverPasswordToken(recoverPassword);

            if (!token)
            {
                return BadRequest(new
                {
                    Message = "Unable to obtain a user reset token"
                });
            }

            return Ok(new {
                Message  = "Please check you inbox for the reset token",
                URI = "user/recover/password",
                HttpType = "PUT",
                Body = new PasswordResetModel
                {
                    Token = "xxxx",
                    Password = "new password",
                    Email = recoverPassword.Email,
                }
            });
        }

        [AllowAnonymous]
        [HttpPut("Recover/Password")]
        public IActionResult ResetPassword([FromBody] PasswordResetModel passwordReset)
        {
            var user = _userService.ResetPassword(passwordReset);

            if(user == null)
            {
                return BadRequest(new
                {
                    Message = "Unable to reset user password please try and make the request again"
                });
            }

            return Ok(user);
        }
    }
}
