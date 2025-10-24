using DAL.Entities.AppUser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities.Models
{
    public class ComplaintandSuggestion:BaseEntity
    {
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public string Problemandsolving { get; set; }
        public DateTime Date { get; set; }

    }
}
