using DAL.Entities.Enums;
using DAL.Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.DTOs.SetUp
{
    public class SetTable
    {
        public int Id { get; set; }
        public string TableNumber { get; set; }
        public int Capacity { get; set; }
        public Location Location { get; set; }
        public string TableImage { get; set; }
        public TableStatus Status { get; set; }
        public ICollection<SetReservation> Reservations { get; set; }
    }
}
