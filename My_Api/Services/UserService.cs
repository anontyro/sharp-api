using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Web;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MimeKit;
using My_Api.Entities;
using My_Api.Models;

namespace My_Api.Services
{

    public interface IUserService
    {
        UserOutputModel Authenticate(string email, string password);
        User DecodeTokenUser(string jwtToken);
        List<User> RemoveSensitiveData(List<User> users);
        User RemoveSensitiveData(User user);
        User Register(RegisterModel nextUser);
        User Activate(string token);
        User ResetPassword(PasswordResetModel passwordReset);
        bool RecoverPasswordToken(RecoverPasswordModel recoverPassword);
        void SendEmail(EmailMessageModel emailMsg);
    }


    public class UserService: IUserService
    {
        private readonly AlexwilkinsonContext _context;
        private readonly IOptions<GmailConfigModel> _emailConfig;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _passwordTokenType = "password-reset-otp";


        public UserService(AlexwilkinsonContext context, IConfiguration configuration, IOptions<GmailConfigModel> emailConfig, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _configuration = configuration;
            _emailConfig = emailConfig;
            _httpContextAccessor = httpContextAccessor;
        }

        public void SendEmail(EmailMessageModel emailMsg)
        {
            var message = new MimeMessage();
            string emailName = _emailConfig.Value.Name;
            string emailUserName = _emailConfig.Value.UserName;
            string emailPassword = _emailConfig.Value.Password;

            message.From.Add(new MailboxAddress(emailName, emailUserName));
            message.To.Add(new MailboxAddress(emailMsg.ToName, emailMsg.ToEmailAddress));
            message.Subject = emailMsg.Subject;
            message.Body = new TextPart("plain")
            {
                Text = emailMsg.Body
            };

            try
            {
                using var client = new SmtpClient();
                client.Connect("smtp.gmail.com", 587);

                client.AuthenticationMechanisms.Remove("XOAUTH2");
                client.Authenticate(emailUserName, emailPassword);

                client.Send(message);
                client.Disconnect(true);
            } catch(Exception err)
            {
                Console.Write(err.Message, err.GetType());
            }


        }

        public bool RecoverPasswordToken(RecoverPasswordModel recoverPassword)
        {
            var user = _context.User
                .Where(u => u.Email == recoverPassword.Email)
                .Where(u => u.IsActive == true)
                .SingleOrDefault();

            if (user == null)
            {
                return false;
            }

            var passwordToken = CreateUserToken(user.Id, _passwordTokenType);


            _context.UserTokens.Add(passwordToken);
            _context.SaveChanges();

            string fullName = user.FirstName + " " + user.LastName;

            var emailMsg = new EmailMessageModel
            {
                ToEmailAddress = user.Email,
                ToName = fullName,
                Subject = "Password recovery",
                Body = string.Format(
                    "To {0}, \n" +
                    "You have requested a password recovery token, please find token attached" +
                    "You must use at the following link: \n" +
                    "PUT user/recover/password \n" +
                    "Body = Token, Password, Email \n" +
                    "Token: {1}"
                    , fullName, passwordToken.TokenValue)
            };

            SendEmail(emailMsg);

            return true;

        }


        public User Register(RegisterModel nextUser)
        {
            try
            {

                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(nextUser.Password, 12);
                User user = new User
                {
                    FirstName = nextUser.FirstName,
                    LastName = nextUser.LastName,
                    Password = hashedPassword,
                    Email = nextUser.Email
                };

                _context.User.Add(user);
                _context.SaveChanges();

                var validToken = CreateUserToken(user.Id);

                _context.UserTokens.Add(validToken);
                _context.SaveChanges();

                string fullName = nextUser.FirstName + " " + nextUser.LastName;
                var activateUri = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}/user/activate?token={HttpUtility.UrlEncode(validToken.TokenValue)}";
                var email = new EmailMessageModel
                {
                    ToEmailAddress = nextUser.Email,
                    ToName = fullName,
                    Subject = "New User Registration for " + fullName,
                    Body = string.Format(
                        "To {0}, \n" +
                        "You have created a new account which is currently inactive please activate account before using \n " +
                        "Click the following link to valid your account: \n" +
                        "{1}"
                        , fullName, activateUri)
                };
                SendEmail(email);
                return RemoveSensitiveData(user);
            }
            catch (Exception err)
            {
                Console.WriteLine(err.GetType());
                return null;
            }
        }

        public User Activate(string token)
        {
            var now = DateTime.Now;
            var userToken = _context.UserTokens
                    .Where(t => t.TokenValue == token)
                    .SingleOrDefault();

            if(userToken == null || now > userToken.ExpirationTime)
            {
                return null;
            }

            var user = _context.User
                .Where(u => u.Id == userToken.UserId)
                .SingleOrDefault();

            if(user == null)
            {
                return null;
            }

            if (user.IsActive)
            {
                return user;
            }

            _context.Update(user);
            user.IsActive = true;
            _context.Remove(userToken);

            _context.SaveChanges();

            return RemoveSensitiveData(user);

        }

        public User ResetPassword(PasswordResetModel passwordReset)
        {
            var now = DateTime.Now;
            var user = _context.User
                .Where(u => u.Email == passwordReset.Email)
                .Where(u => u.IsActive == true)
                .SingleOrDefault();

            if(user == null)
            {
                return null;
            }

            var userToken = _context.UserTokens
                .Where(t => t.TokenValue == passwordReset.Token)
                .Where(t => t.TokenType == _passwordTokenType)
                .OrderByDescending(t => t.DateCreated)
                .FirstOrDefault();

            if(userToken == null)
            {
                return null;
            }

            if(userToken.UserId != user.Id || now > userToken.ExpirationTime)
            {
                return null;
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(passwordReset.Password, 12);

            _context.Update(user);
            user.Password = hashedPassword;
            _context.Remove(userToken);
            _context.SaveChanges();

            return RemoveSensitiveData(user);

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

            var token = CreateJwtToken(user);

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

        private UserToken CreateUserToken(int userId, string tokenType = "activate-otp")
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

        private string CreateJwtToken(User user)
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
