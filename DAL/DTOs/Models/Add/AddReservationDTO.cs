using DAL.Entities.AppUser;
using DAL.Entities.Enums;

namespace DAL.DTOs.Models.Add
{
    public class AddReservationDTO
    {

        public int TableId { get; set; }
        public DateOnly DateOfReservation { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int NumberOfGuests { get; set; }
    }
}
