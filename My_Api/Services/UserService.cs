using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MimeKit;
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
        void SendEmail(EmailMessageModel emailMsg);
    }


    public class UserService: IUserService
    {
        private readonly AlexwilkinsonContext _context;
        private readonly IOptions<GmailConfigModel> _emailConfig;
        private readonly IConfiguration _configuration;


        public UserService(AlexwilkinsonContext context, IConfiguration configuration, IOptions<GmailConfigModel> emailConfig)
        {
            _context = context;
            _configuration = configuration;
            _emailConfig = emailConfig;
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
                string fullName = nextUser.FirstName + " " + nextUser.LastName;
                var email = new EmailMessageModel
                {
                    ToEmailAddress = nextUser.Email,
                    ToName = fullName,
                    Subject = "New User Registration for " + fullName,
                    Body = string.Format("To {0} you have created a new account which is currently inactive please activate account before using", fullName)
                };
                SendEmail(email);
                _context.SaveChanges();
                return RemoveSensitiveData(user);
            }
            catch (Exception err)
            {
                Console.WriteLine(err.GetType());
                return null;
            }
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
