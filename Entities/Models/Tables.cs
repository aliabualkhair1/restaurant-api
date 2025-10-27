using DAL.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities.Models
{
    public class Tables:BaseEntity
    {
        public string TableNumber { get; set; }
        public int Capacity { get; set; }
        public Location Location { get; set; }
        public string TableImage { get; set; }
        public TableStatus Status { get; set; }
        public ICollection<Reservation> Reservations { get; set; }

    }
}
