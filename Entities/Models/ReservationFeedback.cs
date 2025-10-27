using DAL.Entities.AppUser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities.Models
{
    public class ReservationFeedback : BaseEntity
    {
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public int ReservationId { get; set; }
        public Reservation Reservation { get; set; }
        public string Comment { get; set; }
        public int Rating { get; set; }
        public DateTime SubmittedOn { get; set; }
    }
}
