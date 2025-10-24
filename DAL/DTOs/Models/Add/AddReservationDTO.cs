using DAL.Entities.AppUser;
using DAL.Entities.Enums;

namespace DAL.DTOs.Models.Add
{
    public class AddReservationDTO
    {

        public int TableId { get; set; }
        public DateOnly DateOfReservation { get; set; }
        public TimeOnly StartDate { get; set; }
        public TimeOnly EndDate { get; set; }
        public int NumberOfGuests { get; set; }
    }
}
