using System;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using My_Api.Models;

namespace My_Api.Services
{

    public interface IMailService
    {
        void SendEmail(EmailMessageModel emailMsg);
    }

    public class MailService : IMailService
    {
        private readonly IOptions<GmailConfigModel> _emailConfig;

        public MailService(IOptions<GmailConfigModel> emailConfig)
        {
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
            }
            catch (Exception err)
            {
                Console.Write(err.Message, err.GetType());
            }


        }




    }
}
