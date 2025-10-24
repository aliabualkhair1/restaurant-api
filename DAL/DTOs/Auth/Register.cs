using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.DTOs.Auth
{
    public class Register
    {
        [Required]
        public string Username { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        [StringLength(10,MinimumLength =8,ErrorMessage ="Password must be between 8 to 10 letter")]
        public string Password { get; set; }
        [Required]
        [Compare(nameof(Password),ErrorMessage ="Two Passwords Not the same PLZ try again")]
        public string ConfirmPassword { get; set; }
    }
}
