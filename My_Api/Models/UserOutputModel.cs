using System;
using System.ComponentModel.DataAnnotations;


namespace My_Api.Models
{
    public class UserOutputModel : User
    {
    public string Token { get; set; }
        
    }
}
