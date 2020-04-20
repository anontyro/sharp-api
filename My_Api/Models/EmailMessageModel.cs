using System;
namespace My_Api.Models
{
    public class EmailMessageModel
    {
        public EmailMessageModel()
        {
        }


        public string ToEmailAddress { get; set; }
        public string ToName { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }


    }
}
