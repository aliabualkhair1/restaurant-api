using DAL.Entities.AppUser;
using DAL.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities.Models
{
    public class Reservation:BaseEntity
    {
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public int TableId { get; set; }
        public Tables Table { get; set; }
        public DateOnly DateOfReservation { get; set; }
        public TimeOnly StartDate { get; set; }
        public TimeOnly EndDate { get; set; }
        public int NumberOfGuests { get; set; }
        public ReservationStatus ReservationStatus { get; set; }
        public bool IsPaid { get; set; }
    }
}
