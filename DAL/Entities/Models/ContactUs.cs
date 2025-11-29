using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities.Models
{
    public class ContactUs:BaseEntity
    {
        public string FullName  { get; set; }
        public string Email { get; set; }
        public string Message { get; set; }
        public DateTime DateOfSending { get; set; }
    }
}
