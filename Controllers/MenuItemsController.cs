using AutoMapper;
using BLL.Interfaces;
using DAL.DTOs.Models.Add;
using DAL.DTOs.Models.Update;
using DAL.DTOs.SetUp;
using DAL.Entities.Models;
using DAL.Entities.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Restaurant.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "ValidToken")]

    public class MenuItemsController : ControllerBase
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapping;

        public MenuItemsController(IUnitOfWork UnitOfWork, IMapper Mapping)
        {
            unitOfWork = UnitOfWork;
            mapping = Mapping;
        }
        [HttpGet]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public async Task<IActionResult> GetMenuItems(string?MenuName=null,string?ItemName = null,
            int PageNumber=1,int PageSize=10,int? MinPrice=null,int? MaxPrice = null,int? Price = null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            var AllOrders = unitOfWork.Generic<MenuItems>().GetAll().Where(MI=>MI.IsDeleted==false).Select(o => new SetMenuItems
            {
                Id=o.Id,
                MenuName = o.Menu.MenuName,
                ItemName = o.ItemName,
                ItemImage=o.ItemImage,
                Quantity = o.Quantity,
                Price = o.Price,

            });
            if (!string.IsNullOrEmpty(MenuName))
            {
                AllOrders=AllOrders.Where(o => o.MenuName.Contains(MenuName));
            }
            if (!string.IsNullOrEmpty(ItemName))
            {
                AllOrders=AllOrders.Where(o => o.ItemName.Contains(ItemName));
            }
            if (Price.HasValue)
            {
                AllOrders = AllOrders.Where(o => o.Price == Price.Value);
            }
            else if (MinPrice.HasValue && MaxPrice.HasValue)
            {
                AllOrders = AllOrders.Where(o =>o.Price >= MinPrice.Value && o.Price <= MaxPrice.Value);

            }
            else
            {

                if (MinPrice.HasValue)
                {
                    AllOrders = AllOrders.Where(o => o.Price >= MinPrice.Value);
                }
                if (MaxPrice.HasValue)
                {
                    AllOrders = AllOrders.Where(o => o.Price <= MaxPrice.Value);
                }
            }
            var paginatedresult = await Pagination<SetMenuItems>.CreateAsync(AllOrders, PageNumber, PageSize);
            return Ok(paginatedresult);
        }
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin,AdminAssistant,Staff,Customer")]
        public IActionResult GetMenuItemById(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            var menuItem = unitOfWork.Generic<MenuItems>()
                .GetAll()
                .Include(m => m.Menu) 
                .Where(o => o.Id == id && o.IsDeleted == false)
                .Select(o => new SetMenuItems
                {   
                    Id = o.Id,
                    MenuName = o.Menu.MenuName,
                    ItemName = o.ItemName,
                    ItemImage = o.ItemImage,
                    Quantity = o.Quantity,
                    Price = o.Price
                })
                .FirstOrDefault();

            if (menuItem == null)
                return NotFound();

            return Ok(menuItem);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> CreateMenuItem([FromForm] AddMenuItemsDTO OI)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }
            else
            {
                if (OI == null)
                {
                    return BadRequest();
                }
                var imageName = Guid.NewGuid().ToString() + Path.GetExtension(OI.ItemImage.FileName);
                var Folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images");
                Directory.CreateDirectory(Folder);
                var IMGPath = Path.Combine(Folder, imageName);
                using (var stream = new FileStream(IMGPath, FileMode.Create))
                {
                    await OI.ItemImage.CopyToAsync(stream);
                }
                ;
                var mappedItems = mapping.Map<MenuItems>(OI);
                mappedItems.ItemImage = imageName;
                mappedItems.IsDeleted = false;
                unitOfWork.Generic<MenuItems>().Add(mappedItems);
                await unitOfWork.Complete();
                return Ok("Menu item added successfully");
            }
        }
        [HttpPatch("{id:int}")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> UpdateMenuItem([FromForm] UpdateMenuItemsDTO OI, int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            var check = unitOfWork.Generic<MenuItems>().GetById(id);

            if (check == null || check.IsDeleted)
                return NotFound("This Menu Item Not Found");

            var finalMenuId = OI.MenuId ?? check.MenuId;
            var finalItemName = OI.ItemName ?? check.ItemName;
            var finalQuantity = OI.Quantity ?? check.Quantity;
            var finalPrice = OI.Price ?? check.Price;

            string imageName = check.ItemImage;

            check.MenuId = finalMenuId;
            check.ItemName = finalItemName;
            check.Quantity = finalQuantity;
            check.Price = finalPrice;

            if (OI.ItemImage != null)
            {
                if (!string.IsNullOrEmpty(check.ItemImage))
                {
                    var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images", check.ItemImage);
                    if (System.IO.File.Exists(folder))
                        System.IO.File.Delete(folder);
                }

                imageName = Guid.NewGuid().ToString() + Path.GetExtension(OI.ItemImage.FileName);
                var newFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images");
                Directory.CreateDirectory(newFolder);

                var imgPath = Path.Combine(newFolder, imageName);
                using (var stream = new FileStream(imgPath, FileMode.Create))
                {
                    await OI.ItemImage.CopyToAsync(stream);
                }
            }

            check.ItemImage = imageName;

            await unitOfWork.Complete();
            return Ok("Menu item updated successfully");
        }

        [HttpPut("SoftDelete")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> DeleteMenuItem(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }
            else
            {
                var check = unitOfWork.Generic<MenuItems>().GetById(id);
                if (check == null)
                {
                    return NotFound();
                }
                check.IsDeleted = true;
                await unitOfWork.Complete();
                return Ok("Order Item Deleted Successfully");
            }
        }
        [HttpPut("Restore")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> Restore(int id)
        {
            var check = unitOfWork.Generic<MenuItems>().GetById(id);
            if (check == null)
            {
                return NotFound("NotFound");
            }
            check.IsDeleted = false;
            await unitOfWork.Complete();
            return Ok("Restord done");
        }
    }
}
