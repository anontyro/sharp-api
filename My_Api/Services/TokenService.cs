using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using My_Api.Entities;

namespace My_Api.Services
{
    public interface ITokenService
    {
        User DecodeJwtToken(string jwtToken);
        string CreateJwtToken(User user);
        UserToken CreateUserToken(int userId, string tokenType = "activate-otp");

    }
    public class TokenService : ITokenService
    {
        private readonly AlexwilkinsonContext _context;
        private readonly IConfiguration _configuration;


        public TokenService(AlexwilkinsonContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;


        }

        public User DecodeJwtToken(string jwtToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.ReadToken(jwtToken) as JwtSecurityToken;

            var userId = GetSpecificClaim(securityToken);

            if (userId == null)
            {
                return null;
            }

            var user = _context.User
                .Where(x => x.Id == int.Parse(userId))
                .SingleOrDefault();


            user.Password = null;
            return user;
        }

        public string CreateJwtToken(User user)
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

        public UserToken CreateUserToken(int userId, string tokenType = "activate-otp")
        {
            var now = DateTime.Now;
            var verifyToken = BCrypt.Net.BCrypt.HashPassword(now.ToString() + userId.ToString(), 12);
            UserToken token = new UserToken
            {
                ExpirationTime = now.AddHours(1),
                TokenValue = verifyToken,
                UserId = userId,
                TokenType = tokenType,
            };

            return token;
        }

        private string GetSpecificClaim(JwtSecurityToken token, string claimName = "unique_name")
        {
            var userClaim = token.Claims.FirstOrDefault(claim => claim.Type == claimName).Value;

            return userClaim;
        }


    }
}
