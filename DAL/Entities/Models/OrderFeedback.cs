using DAL.Entities.AppUser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities.Models
{
    public class OrderFeedback:BaseEntity
    {
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public int OrderId { get; set; }
        public Orders Order { get; set; }
        public string Comment { get; set; }
        public int Rating { get; set; }
        public DateOnly SubmittedOn { get; set; }
    }
}
