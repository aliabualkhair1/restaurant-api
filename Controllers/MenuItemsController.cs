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
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Restaurant.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenuItemsController : ControllerBase
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapping;

        public MenuItemsController(IUnitOfWork UnitOfWork, IMapper Mapping)
        {
            unitOfWork = UnitOfWork;
            mapping = Mapping;
        }

        [HttpGet("GetMenuItems")]
        public async Task<IActionResult> GetMenuItems(int PageNumber = 1, int PageSize = 20)
        {
            var AllOrders = unitOfWork.Generic<MenuItems>().GetAll().Where(MI => MI.IsDeleted == false && MI.Menu.IsDeleted == false && MI.Quantity > 0).Include(mi=>mi.Menu).Select(o => new SetMenuItems
            {
                Id = o.Id,
                MenuId = o.MenuId,
                MenuName = o.Menu.MenuName,
                ItemName = o.ItemName,
                ItemImage = o.ItemImage,
                Quantity = o.Quantity,
                Price = o.Price,
            });
            var paginatedresult = await Pagination<SetMenuItems>.CreateAsync(AllOrders, PageNumber, PageSize);
            return Ok(paginatedresult);
        }

        [HttpGet("GetDeltedMenuItems")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> GetDeltedMenuItems(int PageNumber = 1, int PageSize = 20)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            var AllOrders = unitOfWork.Generic<MenuItems>().GetAll().Where(MI => MI.IsDeleted == true && MI.Menu.IsDeleted == false).Select(o => new SetMenuItems
            {
                Id = o.Id,
                MenuId = o.MenuId,
                MenuName = o.Menu.MenuName,
                ItemName = o.ItemName,
                ItemImage = o.ItemImage,
                Quantity = o.Quantity,
                Price = o.Price,
            });
            var paginatedresult = await Pagination<SetMenuItems>.CreateAsync(AllOrders, PageNumber, PageSize);
            return Ok(paginatedresult);
        }

        [HttpGet("GetMenuItemsByFilteration")]
        public async Task<IActionResult> GetMenuItemsByFilteration(int PageNumber = 1, int PageSize = 20, string? MenuName = null, string? ItemName = null, int? MinPrice = null, int? MaxPrice = null, int? Price = null)
        {
            var MenuItems = unitOfWork.Generic<MenuItems>().GetAll().Where(MI => MI.IsDeleted == false && MI.Menu.IsDeleted == false && MI.Quantity > 0).Select(o => new SetMenuItems
            {
                Id = o.Id,
                MenuId = o.MenuId,
                MenuName = o.Menu.MenuName,
                ItemName = o.ItemName,
                ItemImage = o.ItemImage,
                Quantity = o.Quantity,
                Price = o.Price,

            });
            if (!string.IsNullOrEmpty(MenuName))
            {
                MenuItems = MenuItems.Where(o => o.MenuName.Contains(MenuName));
            }
            if (!string.IsNullOrEmpty(ItemName))
            {
                MenuItems = MenuItems.Where(o => o.ItemName.Contains(ItemName));
            }
            if (Price.HasValue)
            {
                MenuItems = MenuItems.Where(o => o.Price == Price.Value);
            }
            else if (MinPrice.HasValue && MaxPrice.HasValue)
            {
                MenuItems = MenuItems.Where(o => o.Price >= MinPrice.Value && o.Price <= MaxPrice.Value);

            }
            else
            {

                if (MinPrice.HasValue)
                {
                    MenuItems = MenuItems.Where(o => o.Price >= MinPrice.Value);
                }
                if (MaxPrice.HasValue)
                {
                    MenuItems = MenuItems.Where(o => o.Price <= MaxPrice.Value);
                }
            }
            var paginatedresult = await Pagination<SetMenuItems>.CreateAsync(MenuItems, PageNumber, PageSize);
            return Ok(paginatedresult);
        }

        [HttpGet("GetUnAvailableMenuItemsByFilteration")]
        [Authorize(Roles = "Admin,AdminAssistant,Staff,Customer")]
        public async Task<IActionResult> GetUnAvailableMenuItemsByFilteration(int PageNumber = 1, int PageSize = 20, string? MenuName = null, string? ItemName = null)
        {
            var MenuItems = unitOfWork.Generic<MenuItems>().GetAll().Where(MI => MI.IsDeleted == false && MI.Menu.IsDeleted == false && MI.IsAvailable == false).Select(o => new SetMenuItems
            {
                Id = o.Id,
                MenuId = o.MenuId,
                MenuName = o.Menu.MenuName,
                ItemName = o.ItemName,
                ItemImage = o.ItemImage,
                Quantity = o.Quantity,
                Price = o.Price,

            });
            if (!string.IsNullOrEmpty(MenuName))
            {
                MenuItems = MenuItems.Where(o => o.MenuName.Contains(MenuName));
            }
            if (!string.IsNullOrEmpty(ItemName))
            {
                MenuItems = MenuItems.Where(o => o.ItemName.Contains(ItemName));
            }
            var paginatedresult = await Pagination<SetMenuItems>.CreateAsync(MenuItems, PageNumber, PageSize);
            return Ok(paginatedresult);
        }

        [HttpGet("GetDeletedMenuItemsByFilteration")]
        [Authorize(Roles = "Admin,AdminAssistant,Staff,Customer")]
        public async Task<IActionResult> GetDeletedMenuItemsByFilteration(int PageNumber = 1, int PageSize = 20, string? MenuName = null, string? ItemName = null, int? MinPrice = null, int? MaxPrice = null, int? Price = null)
        {
            var MenuItems = unitOfWork.Generic<MenuItems>().GetAll().Where(MI => MI.IsDeleted == true && MI.Menu.IsDeleted == false).Select(o => new SetMenuItems
            {
                Id = o.Id,
                MenuId = o.MenuId,
                MenuName = o.Menu.MenuName,
                ItemName = o.ItemName,
                ItemImage = o.ItemImage,
                Quantity = o.Quantity,
                Price = o.Price,

            });
            if (!string.IsNullOrEmpty(MenuName))
            {
                MenuItems = MenuItems.Where(o => o.MenuName.Contains(MenuName));
            }
            if (!string.IsNullOrEmpty(ItemName))
            {
                MenuItems = MenuItems.Where(o => o.ItemName.Contains(ItemName));
            }
            if (Price.HasValue)
            {
                MenuItems = MenuItems.Where(o => o.Price == Price.Value);
            }
            else if (MinPrice.HasValue && MaxPrice.HasValue)
            {
                MenuItems = MenuItems.Where(o => o.Price >= MinPrice.Value && o.Price <= MaxPrice.Value);

            }
            else
            {

                if (MinPrice.HasValue)
                {
                    MenuItems = MenuItems.Where(o => o.Price >= MinPrice.Value);
                }
                if (MaxPrice.HasValue)
                {
                    MenuItems = MenuItems.Where(o => o.Price <= MaxPrice.Value);
                }
            }
            var paginatedresult = await Pagination<SetMenuItems>.CreateAsync(MenuItems, PageNumber, PageSize);
            return Ok(paginatedresult);
        }

        [HttpGet("getbyid/{id:int}")]
        public async Task<IActionResult> GetMenuItemsByMenuId(int id, int PageNumber = 1, int PageSize = 20, string ItemName = null, int? MinPrice = null, int? MaxPrice = null, int? Price = null)
        {
            var MenuItems = unitOfWork.Generic<MenuItems>().GetAll().Include(mi=>mi.Menu).Where(o => o.MenuId == id && o.IsDeleted == false && o.Menu.IsDeleted == false && o.Quantity > 0).Select(o => new SetMenuItems
            {
                Id = o.Id,
                MenuId = o.MenuId,
                MenuName = o.Menu.MenuName,
                ItemName = o.ItemName,
                ItemImage = o.ItemImage,
                Quantity = o.Quantity,
                Price = o.Price,
                IsAvailable=o.IsAvailable
            });

            if (!string.IsNullOrEmpty(ItemName))
            {
                MenuItems = MenuItems.Where(o => o.ItemName.Contains(ItemName));
            }
            if (Price.HasValue)
            {
                MenuItems = MenuItems.Where(o => o.Price == Price.Value);
            }
            else
            {
                if (MinPrice.HasValue)
                {
                    MenuItems = MenuItems.Where(o => o.Price >= MinPrice.Value);
                }
                if (MaxPrice.HasValue)
                {
                    MenuItems = MenuItems.Where(o => o.Price <= MaxPrice.Value);
                }
            }
            var paginatedresult = await Pagination<SetMenuItems>.CreateAsync(MenuItems, PageNumber, PageSize);
            return Ok(paginatedresult);
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
                    return BadRequest("البيانات المرسلة فارغة أو غير صحيحة.");
                }
                var imageName = Guid.NewGuid().ToString() + Path.GetExtension(OI.ItemImage.FileName);
                var Folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images");
                Directory.CreateDirectory(Folder);
                var IMGPath = Path.Combine(Folder, imageName);
                using (var stream = new FileStream(IMGPath, FileMode.Create))
                {
                    await OI.ItemImage.CopyToAsync(stream);
                }

                var mappedItems = mapping.Map<MenuItems>(OI);
                mappedItems.ItemImage = imageName;
                mappedItems.IsDeleted = false;
                mappedItems.IsAvailable = true;
                unitOfWork.Generic<MenuItems>().Add(mappedItems);
                await unitOfWork.Complete();
                return Ok("تمت إضافة المنتج بنجاح.");
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
                return NotFound("لم يتم العثور على المنتج.");

            var finalMenuId =check.MenuId;
            var finalItemName = OI.ItemName ?? check.ItemName;
            var finalQuantity = OI.Quantity ?? check.Quantity;
            var finalPrice = OI.Price ?? check.Price;

            string imageName = check.ItemImage;

            check.MenuId = finalMenuId;
            check.ItemName = finalItemName;


            if (finalQuantity == 0)
            {
                check.IsAvailable = false;
            }
            else
            {
                check.IsAvailable = true;
            }

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
            return Ok("تم تحديث المنتج بنجاح.");
        }

        [HttpGet("GetUnAvailableMenuItems")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> GetUnAvailableMenuItems(int PageNumber = 1, int PageSize = 20)
        {
            var userId = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            var AllOrders = unitOfWork.Generic<MenuItems>().GetAll().Where(MI => MI.IsDeleted == false && MI.Menu.IsDeleted == false && MI.IsAvailable == false).Select(o => new SetMenuItems
            {
                Id = o.Id,
                MenuId = o.MenuId,
                MenuName = o.Menu.MenuName,
                ItemName = o.ItemName,
                ItemImage = o.ItemImage,
                Quantity = o.Quantity,
                Price = o.Price,
                IsAvailable=o.IsAvailable
            });
            var paginatedresult = await Pagination<SetMenuItems>.CreateAsync(AllOrders, PageNumber, PageSize);
            return Ok(paginatedresult);
        }

        [HttpPut("GetMenuItemsAvailable")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> GetMenuItemsAvailable(int id, AvailableMenuItems available)
        {
            var check = unitOfWork.Generic<MenuItems>().GetById(id);
            if (check == null)
            {
                return NotFound("لم يتم العثور على المنتج.");
            }
            if (available.Quantity > 0)
            {
                check.Quantity = available.Quantity;
                check.IsAvailable = true;
                await unitOfWork.Complete();
                return Ok("تمت استعادة المنتج بنجاح.");
            }
            else
            {
                return BadRequest("يجب إدخال كمية أكبر من الصفر.");
            }
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
                    return NotFound("لم يتم العثور على المنتج .");
                }
                check.IsDeleted = true;
                check.IsAvailable = false;
                await unitOfWork.Complete();
                return Ok("تم حذف المنتج بنجاح.");
            }
        }

        [HttpPut("Restore")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> Restore(int id)
        {
            var check = unitOfWork.Generic<MenuItems>().GetById(id);
            if (check == null)
            {
                return NotFound("لم يتم العثور على المنتج .");
            }
            check.IsDeleted = false;
            check.IsAvailable = true;
            await unitOfWork.Complete();
            return Ok("تمت استعادة المنتج بنجاح.");
        }
    }
}