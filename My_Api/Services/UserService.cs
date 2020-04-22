using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using My_Api.Models;

namespace My_Api.Services
{

    public interface IUserService
    {
        UserOutputModel Authenticate(string email, string password);
        List<User> RemoveSensitiveData(List<User> users);
        User RemoveSensitiveData(User user);
        User Register(RegisterModel nextUser);
        User Activate(string token);
        User ResetPassword(PasswordResetModel passwordReset);
        bool RecoverPasswordToken(RecoverPasswordModel recoverPassword);
    }


    public class UserService: IUserService
    {
        private readonly AlexwilkinsonContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMailService _mailService;
        private readonly ITokenService _tokenService;
        private readonly string _passwordTokenType = "password-reset-otp";


        public UserService(AlexwilkinsonContext context, IConfiguration configuration, IMailService mailService, IHttpContextAccessor httpContextAccessor, ITokenService tokenService)
        {
            _context = context;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _mailService = mailService;
            _tokenService = tokenService;
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

            var passwordToken = _tokenService.CreateUserToken(user.Id, _passwordTokenType);


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

            _mailService.SendEmail(emailMsg);

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

                var validToken =_tokenService.CreateUserToken(user.Id);

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
                _mailService.SendEmail(email);
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

            var token = _tokenService.CreateJwtToken(user);

            UserOutputModel output = new UserOutputModel(user, token);

            return output;
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



    }
}
