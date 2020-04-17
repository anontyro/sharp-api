using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using My_Api.Models;

namespace My_Api.Services
{

    public interface IUserService
    {
        UserOutputModel Authenticate(string email, string password);
    }


    public class UserService: IUserService
    {
        private readonly AlexwilkinsonContext _context;
        private readonly IConfiguration _configuration;


        public UserService(AlexwilkinsonContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public UserOutputModel Authenticate(string email, string password)
        {
            var user = _context.User
                .Where(u => u.IsActive == true)
                .Where(u => u.Email == email)
                .SingleOrDefault();

            if(user == null)
            {
                return null;
            }


            bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(password, user.Password);

            if (!isPasswordCorrect)
            {
                return null;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration.GetValue<string>("JwtSecret"));
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]{
                    new Claim(ClaimTypes.Name, user.Id.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            UserOutputModel output = new UserOutputModel
            {
                FirstName = user.FirstName,
                Email = user.Email,
                IsActive = user.IsActive,
                Token = tokenHandler.WriteToken(token),

            };

            return output;
        }


    }
}
