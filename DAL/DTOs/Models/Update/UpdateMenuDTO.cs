using DAL.Entities.Models;
using Microsoft.AspNetCore.Http;

namespace DAL.DTOs.Models.Update
{
    public class UpdateMenuDTO
    {
        public int? CategoryId { get; set; }
        public string? MenuName { get; set; }
        public string? Description { get; set; }
        public bool? IsAvailable { get; set; }
    }
}
