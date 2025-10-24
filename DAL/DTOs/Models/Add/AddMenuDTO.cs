using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.DTOs.Models.Add
{
    public class AddMenuDTO
    {
        public int CategoryId { get; set; }
        public string MenuName { get; set; }
        public string Description { get; set; }
    }
}
