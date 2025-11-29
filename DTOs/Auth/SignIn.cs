using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.DTOs.Auth
{
    public class SignIn
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
