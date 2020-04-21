using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace My_Api.Entities
{
    public class UserToken
    {

        public int Id {get;set;}
        public string TokenValue { get; set; }
        public string TokenType { get; set; } = "OTP";
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime DateCreated { get; set; }
        public int UserId { get; set; }
        public DateTime ExpirationTime { get; set; }
    }
}
