using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.DTOs.Models.Update
{
    public class UpdateReservationDTO
    {
        public int? TableId { get; set; }
        public DateOnly? DateOfReservation { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public int? NumberOfGuests { get; set; }
    }
}
