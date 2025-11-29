using DAL.Entities.AppUser;
using DAL.Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.DTOs.SetUp
{
    public class SetMenuItems
    {
        public int Id { get; set; }
        public int MenuId { get; set; }
        public string MenuName { get; set; }
        public string ItemName { get; set; }
        public string ItemImage { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; }
    }
}
