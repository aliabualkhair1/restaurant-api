using DAL.DTOs.SetUp;
using DAL.Entities.AppUser;
using DAL.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace DAL.Entities.Models
{
    public class Orders:BaseEntity
    {
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public DateTime OrderDate { get; set; }
        public OrderStatus Status { get; set; }
        public ICollection<OrderItems> OrderItems { get; set; }
        public decimal TotalPrice { get; set; }
        public bool IsPaid { get; set; }
    }
}
