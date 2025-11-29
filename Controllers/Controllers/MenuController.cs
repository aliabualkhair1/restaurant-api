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
using static System.Net.Mime.MediaTypeNames;

namespace Restaurant.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenuController : ControllerBase
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapping;
        public MenuController(IUnitOfWork UnitOfWork, IMapper Mapping)
        {
            unitOfWork = UnitOfWork;
            mapping = Mapping;
        }

        [HttpGet]
        public async Task<IActionResult> GetMenu(string? categoryname = null, int PageNumber = 1, int PageSize = 20)
        {
            var allMenus = unitOfWork.Generic<Menu>().GetAll()
                .Where(m => !m.IsDeleted&&!m.Category.IsDeleted)
                .Include(m => m.MenuItems);

            foreach (var menu in allMenus)
            {
                if (menu.MenuItems.All(sd => sd.IsDeleted||!sd.IsAvailable))
                    menu.IsAvailable = false;
                else menu.IsAvailable = true;
            }

           await unitOfWork.Complete();

            var Menu = allMenus
                .Where(m => m.IsAvailable && m.MenuItems.Any());

            if (!string.IsNullOrEmpty(categoryname))
            {
                Menu = Menu.Where(m =>
                    m.Category.Name.ToLower() == categoryname.ToLower().Trim());
            }

            var result = Menu.Select(m => new SetMenu
            {
                Id = m.Id,
                CategoryId = m.CategoryId,
                CategoryName = m.Category.Name,
                Name = m.MenuName,
                Description = m.Description,
                IsAvailable = m.IsAvailable,
                MenuItems = m.MenuItems.Select(oi => new SetMenuItems
                {
                    Id = oi.Id,
                    MenuId = oi.MenuId,
                    MenuName = oi.Menu.MenuName,
                    ItemName = oi.ItemName,
                    ItemImage = oi.ItemImage,
                    Quantity = oi.Quantity,
                    Price = oi.Price
                }).ToList()
            });

            var paginatedResult = await Pagination<SetMenu>.CreateAsync(result, PageNumber, PageSize);
            return Ok(paginatedResult);
        }


        [HttpGet("GetMenuByName")]
        public IActionResult GetMenuByName(string? name)
        {
            var menu = unitOfWork.Generic<Menu>()
                .GetAll()
                .Include(m => m.Category)
                .Include(m => m.MenuItems)
                .Where(m => m.MenuName == name && m.IsDeleted == false && m.IsAvailable == true&&!m.Category.IsDeleted)
                .Select(m => new SetMenu
                {
                    Id = m.Id,
                    CategoryId = m.Category.Id,
                    CategoryName = m.Category.Name,
                    Name = m.MenuName,
                    Description = m.Description,
                    IsAvailable = m.IsAvailable,
                    MenuItems = m.MenuItems.Select(oi => new SetMenuItems
                    {
                        Id = oi.Id,
                        MenuId = oi.MenuId,
                        MenuName = oi.Menu.MenuName,
                        ItemName = oi.ItemName,
                        ItemImage = oi.ItemImage,
                        Quantity = oi.Quantity,
                        Price = oi.Price
                    }).ToList()
                }).ToList();

            if (!menu.Any())
                return NotFound("المينيو غير موجود.");

            return Ok(menu);
        }

        [HttpGet("GetDeletedMenuName")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public IActionResult GetDeletedMenuName(string? name)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }
            var menu = unitOfWork.Generic<Menu>().GetAll().Where(m => m.IsDeleted == true&&!m.Category.IsDeleted && m.MenuName == name).Select(m => new SetMenu
            {
                Id = m.Id,
                Name = m.MenuName,
                CategoryName = m.Category.Name,
                Description = m.Description,
                IsDeleted = m.IsDeleted,
                IsAvailable = m.IsAvailable

            });

            if (!menu.Any())
            {
                return NotFound("المينيو غير موجود.");
            }

            return Ok(menu);
        }

        [HttpGet("GetUnAvailableMenuName")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public IActionResult GetUnAvailableMenuName(string? name)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }
            var menu = unitOfWork.Generic<Menu>().GetAll().Include(m=>m.MenuItems).Where(m => m.IsDeleted == false && m.IsAvailable == false&&!m.Category.IsDeleted && m.MenuName == name).Select(m => new SetMenu
            {
                Id = m.Id,
                Name = m.MenuName,
                CategoryName = m.Category.Name,
                Description = m.Description,
                IsDeleted = m.IsDeleted,
                IsAvailable = m.IsAvailable,
                MenuItems =m.MenuItems.Select(mi=>new SetMenuItems
                {
                    Id = mi.Id,
                    MenuId = mi.MenuId,
                    MenuName = m.MenuName,
                    ItemName = mi.ItemName,
                    ItemImage = mi.ItemImage,
                    Quantity = mi.Quantity,
                    Price = mi.Price,
                    IsAvailable=mi.IsAvailable
                }).ToList()
            });

            if (!menu.Any())
            {
                return NotFound("المينيو غير موجود.");
            }

            return Ok(menu);
        }

        [HttpPost]
        [Authorize(Roles = "AdminAssistant,Admin")]
        public async Task<IActionResult> AddMenu(AddMenuDTO Menu)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }
            else
            {
                if (Menu == null)
                {
                    return BadRequest("البيانات المرسلة فارغة أو غير صحيحة.");
                }

                var mappedMenu = mapping.Map<Menu>(Menu);
                mappedMenu.IsDeleted = false;
                mappedMenu.CreatedAt = DateTime.Now;
                mappedMenu.IsAvailable = true;
                unitOfWork.Generic<Menu>().Add(mappedMenu);
                await unitOfWork.Complete();
                return Ok("تمت إضافة المينيو بنجاح.");
            }
        }

        [HttpPatch("{id:int}")]
        [Authorize(Roles = "AdminAssistant,Admin")]
        public async Task<IActionResult> UpdateMenu([FromBody] UpdateMenuDTO Menu, int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            var check = unitOfWork.Generic<Menu>().GetById(id);

            if (check == null || check.IsDeleted == true)
            {
                return NotFound("هذا المينيو غير موجود.");
            }

            var finalCategoryId = Menu.CategoryId ?? check.CategoryId;
            var finalMenuName = Menu.MenuName ?? check.MenuName;
            var finalDescription = Menu.Description ?? check.Description;
            var availabilitystatus = Menu.IsAvailable ?? check.IsAvailable;
            check.CategoryId = finalCategoryId;
            check.MenuName = finalMenuName;
            check.Description = finalDescription;
            check.IsAvailable = availabilitystatus;
            check.UpdatedAt = DateTime.Now;

            await unitOfWork.Complete();
            return Ok("تم تحديث المينيو بنجاح.");
        }

        [HttpPut("SoftDelete")]
        [Authorize(Roles = "AdminAssistant,Admin")]
        public async Task<IActionResult> RemoveMenu(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }
            else
            {
                var check = unitOfWork.Generic<Menu>().GetById(id);
                if (check == null)
                {
                    return NotFound("المينيو غير موجود.");
                }
                var menuitems = unitOfWork.Generic<MenuItems>().GetAll().Where(mi => mi.MenuId == check.Id);
                foreach (var i in menuitems)
                {
                    i.IsAvailable = false;
                    i.IsDeleted = true;
                }
                check.IsDeleted = true;
                check.IsAvailable = false;
                await unitOfWork.Complete();
                return Ok("تم حذف المينيو بنجاح.");
            }
        }

        [HttpGet("GetDeletedMenu")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public IActionResult GetDeletedMenu()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {

                var Menu = unitOfWork.Generic<Menu>().GetAll().Where(m => m.IsDeleted == true && m.IsAvailable == false).Select(m => new SetMenu
                {
                    Id = m.Id,
                    CategoryId = m.CategoryId,
                    CategoryName = m.Category.Name,
                    Name = m.MenuName,
                    Description = m.Description,
                    IsAvailable = m.IsAvailable,
                    MenuItems = m.MenuItems.Select(oi => new SetMenuItems
                    {
                        Id = oi.Id,
                        MenuName = oi.Menu.MenuName,
                        ItemName = oi.ItemName,
                        ItemImage = oi.ItemImage,
                        Quantity = oi.Quantity,
                        Price = oi.Price,
                    }).ToList()
                });
                return Ok(Menu);
            }
            return BadRequest("غير مسموح لك بالدخول");
        }

        [HttpGet("GetUnAvailableMenu")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public IActionResult GetUnAvailableMenu()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {

                var Menu = unitOfWork.Generic<Menu>().GetAll().Where(m => m.IsDeleted == false && m.IsAvailable == false).Select(m => new SetMenu
                {
                    Id = m.Id,
                    CategoryId = m.CategoryId,
                    CategoryName = m.Category.Name,
                    Name = m.MenuName,
                    Description = m.Description,
                    IsAvailable = m.IsAvailable,
                    MenuItems = m.MenuItems.Select(oi => new SetMenuItems
                    {
                        Id = oi.Id,
                        MenuName = oi.Menu.MenuName,
                        ItemName = oi.ItemName,
                        ItemImage = oi.ItemImage,
                        Quantity = oi.Quantity,
                        Price = oi.Price,
                        IsAvailable=oi.IsAvailable,
                        MenuId=oi.MenuId
                    }).ToList()
                });
                return Ok(Menu);
            }
            return BadRequest("غير مسموح لك بالدخول");
        }

        [HttpPut("Available")]
        [Authorize(Roles = "AdminAssistant,Admin")]
        public async Task<IActionResult> Available(int id)
        {
            var check = unitOfWork.Generic<Menu>().GetById(id);
            if (check == null)
            {
                return NotFound("لم يتم العثور على المينيو.");
            }
            check.IsAvailable = true;
            await unitOfWork.Complete();
            return Ok("أصبح المينيو متاح الآن.");
        }

        [HttpPut("Restore")]
        [Authorize(Roles = "AdminAssistant,Admin")]
        public async Task<IActionResult> Restore(int id)
        {
            var check = unitOfWork.Generic<Menu>().GetById(id);
            if (check == null)
            {
                return NotFound("لم يتم العثور على المينيو.");
            }
            var menuitems = unitOfWork.Generic<MenuItems>().GetAll().Where(mi => mi.MenuId == check.Id);
            foreach (var i in menuitems)
            {
                i.IsAvailable = true;
                i.IsDeleted = false;
            }
            check.IsDeleted = false;
            check.IsAvailable = true;
            await unitOfWork.Complete();
            return Ok("تمت استعادة المينيو بنجاح.");
        }
    }
}