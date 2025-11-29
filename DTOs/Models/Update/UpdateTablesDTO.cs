using DAL.Entities.Enums;
using Microsoft.AspNetCore.Http;

namespace DAL.DTOs.Models.Update
{
    public class UpdateTablesDTO
    {
        public string? TableNumber { get; set; }
        public int? Capacity { get; set; }
        public Location? Location { get; set; }
        public IFormFile? TableImage { get; set; }
    }
}
