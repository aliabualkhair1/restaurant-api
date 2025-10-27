using DAL.Entities.AppUser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities.Models
{
    public class MenuItems:BaseEntity
    {

        public int MenuId { get; set; }
        public Menu Menu { get; set; }
        public string  ItemName{ get; set; }
        public string ItemImage { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public ICollection<OrderItems> OrderItems { get; set; }
    }
}
