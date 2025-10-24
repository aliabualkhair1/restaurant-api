using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities.AppUser
{
    public class ApplicationUser:IdentityUser
    {
        public bool IsDeleted { get; set; }
        public int TokenVersion { get; set; } = 1;
    }
}
