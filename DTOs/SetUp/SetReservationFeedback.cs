using DAL.Entities.AppUser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.DTOs.SetUp
{
    public class SetReservationFeedback
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Username { get; set; }
        public int ReservationId { get; set; }
        public DateOnly ReservationDate { get; set; }
        public string Comment { get; set; }
        public int Rating { get; set; }
        public DateTime SubmittedOn { get; set; }
    }
}
