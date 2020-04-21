using System;
using System.ComponentModel.DataAnnotations;

namespace My_Api.Models
{
    public class PasswordResetModel
    {
        public PasswordResetModel()
        {
        }

        [Required]
        public string Token { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public string Email { get; set; }
    }
}
