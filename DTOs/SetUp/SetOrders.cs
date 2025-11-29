using DAL.Entities.AppUser;
using DAL.Entities.Enums;
using DAL.Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.DTOs.SetUp
{
    public class SetOrders
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public int OrderId { get; set; }
        public DateOnly OrderDate { get; set; }
        public OrderStatus Status { get; set; }
        public ICollection<SetOrderItems> OrderItems { get; set; }
        public decimal TotalPrice { get; set; }
        public bool IsPaid { get; set; }
    }
}
