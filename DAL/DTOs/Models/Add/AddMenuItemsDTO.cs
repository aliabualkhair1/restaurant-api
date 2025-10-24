using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.DTOs.Models.Add
{
    public class AddMenuItemsDTO
    {
        public int MenuId { get; set; }
        public string ItemName { get; set; }
        public IFormFile ItemImage { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
