using Microsoft.AspNetCore.Http;

namespace DAL.DTOs.Models.Update
{
    public class UpdateMenuItemsDTO
    {
        public string? ItemName { get; set; }
        public IFormFile? ItemImage { get; set; }
        public int? Quantity { get; set; }
        public decimal? Price { get; set; }
    }
}
