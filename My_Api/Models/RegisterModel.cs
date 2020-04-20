using System;
using System.ComponentModel.DataAnnotations;

namespace My_Api.Models
{
    public class RegisterModel : AuthenticateModel
    {
        public RegisterModel()
        {
            IsActive = false;
        }

        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        public bool IsActive { get; }

    }
}
