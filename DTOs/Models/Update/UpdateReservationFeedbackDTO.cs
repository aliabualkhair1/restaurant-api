using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.DTOs.Models.Update
{
    public class UpdateReservationFeedbackDTO
    {
        public string? Comment { get; set; }
        public int? Rating { get; set; }
    }
}
