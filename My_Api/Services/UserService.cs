using System;
using System.Collections.Generic;
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
        User DecodeTokenUser(string jwtToken);
        List<User> RemoveSensitiveData(List<User> users);
        User RemoveSensitiveData(User user);
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

            var token = CreateNextToken(user);

            UserOutputModel output = new UserOutputModel(user, token);

            return output;
        }

        public User DecodeTokenUser(string jwtToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.ReadToken(jwtToken) as JwtSecurityToken;

            var userId = GetSpecificClaim(securityToken);

            if(userId == null)
            {
                return null;
            }

            var user = _context.User
                .Where(x => x.Id == int.Parse(userId))
                .SingleOrDefault();


            user.Password = null;
            return user;
        }

        public List<User> RemoveSensitiveData(List<User> users)
        {
            users.ForEach(u =>
            {
                RemoveSensitiveData(u);
            });

            return users;
        }

        public User RemoveSensitiveData(User user)
        {
            user.Password = null;

            return user;
        }

        private string GetSpecificClaim(JwtSecurityToken token, string claimName = "unique_name")
        {
            var userClaim = token.Claims.FirstOrDefault(claim => claim.Type == claimName).Value;

            return userClaim;
        }

        private string CreateNextToken(User user)
        {
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

            return tokenHandler.WriteToken(token);
        }

    }
}
