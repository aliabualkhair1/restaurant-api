using DAL.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.DTOs.Auth
{
    public class UpdateRole
    {
        public string username { get; set; }
        public RoleEnum newrole { get; set; }
    }
}
