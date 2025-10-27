using DAL.Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.DTOs.SetUp
{
    public class SetCategory
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<SetMenu> Menu { get; set; }
    }
}
