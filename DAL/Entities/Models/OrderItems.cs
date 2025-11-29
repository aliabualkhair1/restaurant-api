using DAL.Entities.AppUser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities.Models
{
    public class OrderItems:BaseEntity
    {
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public int OrderId { get; set; }
        public Orders Order { get; set; }
        public int MenuItemId { get; set; }       
        public MenuItems MenuItem { get; set; }
        public string ItemName { get; set; }      
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal SubTotal { get; set; }
        public DateTime? DeletionDate { get; set; } = null;
        public bool IsPermanentDelete { get; set; } = false;
    }
}
