using System;
using System.ComponentModel.DataAnnotations;


namespace My_Api.Models
{
    public class UserOutputModel : User
    {
        public UserOutputModel(User user, string token)
        {
            Id = user.Id;
            FirstName = user.FirstName;
            LastName = user.LastName;
            Email = user.Email;
            Password = null;
            IsActive = user.IsActive;
            Token = token;
        }
        public UserOutputModel()
        {

        }
        public string Token { get; set; }
        
        }
}
