using DAL.Entities.AppUser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.DTOs.Models.Add
{
    public class AddReservationFeedbackDTO
    {
        public int ReservationId { get; set; }
        public string Comment { get; set; }
        public int Rating { get; set; }
    }
}
