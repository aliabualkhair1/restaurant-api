using DAL.Entities.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.DTOs.Models.Add
{
    public class AddTablesDTO
    {
        public string TableNumber { get; set; }
        public int Capacity { get; set; }
        public Location Location { get; set; }
        public IFormFile TableImage { get; set; }
    }
}
