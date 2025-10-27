﻿using DAL.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.DTOs.SetUp
{
    public class SetReservation
    {
        public int ReservationId { get; set; }
        public ReservationStatus Status { get; set; }
        public string UserId { get; set; }
        public string Username { get; set; }
        public string TableNumber { get; set; }
        public Location TableLocation  { get; set; }
        public int NumberOfGuests { get; set; }
        public DateOnly DateOfReservation { get; set; }
        public TimeOnly StartDate { get; set; }
        public TimeOnly EndDate { get; set; }
    }
}
