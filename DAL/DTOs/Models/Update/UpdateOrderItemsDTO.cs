using DAL.Entities.Enums;
namespace DAL.DTOs.Models.Update
{
    public class UpdateOrderItemsDTO
    {
        public int? MenuItemId { get; set; }
        public int? Quantity { get; set; }
    }
}
