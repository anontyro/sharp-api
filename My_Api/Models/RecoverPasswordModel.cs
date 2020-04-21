using System;
using System.ComponentModel.DataAnnotations;

namespace My_Api.Models
{
    public class RecoverPasswordModel
    {
        public RecoverPasswordModel()
        {
        }
        [Required]
        public string Email { get; set; }
    }
}
