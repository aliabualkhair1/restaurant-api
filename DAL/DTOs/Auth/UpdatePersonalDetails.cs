using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.DTOs.Auth
{
    public class UpdatePersonalDetails
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? NationalId { get; set; }
        public string? Username { get; set; }
         public string? Email { get; set; }
        [StringLength(10, MinimumLength = 8, ErrorMessage = "Password must be between 8 to 10 letter")]
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
        [Compare(nameof(NewPassword), ErrorMessage = "Two Passwords Not the same PLZ try again")]
        public string? ConfirmPassword { get; set; }
    }
}

