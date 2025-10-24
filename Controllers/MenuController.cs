using AutoMapper;
using BLL.Interfaces;
using DAL.DTOs.Models.Add;
using DAL.DTOs.Models.Update;
using DAL.DTOs.SetUp;
using DAL.Entities.Models;
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
    [Authorize(Policy = "ValidToken")]

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
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public IActionResult GetMenu(int? categoryid=null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {

                var Menu = unitOfWork.Generic<Menu>().GetAll().Where(m=>m.IsDeleted==false).Select(m => new SetMenu
                {
                    Id=m.Id,
                    CategoryId=m.Category.Id,
                    Name=m.MenuName,
                    Description = m.Description,
                    IsAvailable = m.IsAvailable,
                    MenuItems = m.MenuItems.Select(oi => new SetMenuItems
                    {
                        Id=oi.Id,
                        MenuName=oi.Menu.MenuName,
                        ItemName = oi.ItemName,
                        ItemImage = oi.ItemImage,
                        Quantity = oi.Quantity,
                        Price = oi.Price,
                    }).ToList()
                });
                if (categoryid.HasValue)
                {
                    Menu=Menu.Where(m=>m.CategoryId==categoryid.Value);
                }
                    return Ok(Menu);
            }
            return Unauthorized();
        }
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin,Staff,Customer,AdminAssistant")]
        public IActionResult GetMenuById(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            var menu = unitOfWork.Generic<Menu>()
                .GetAll()
                .Include(m => m.Category)
                .Include(m => m.MenuItems)
                .Where(m => m.Id == id&&m.IsDeleted==false)
                .Select(m => new SetMenu
                {   
                    Id = m.Id,
                    CategoryId = m.Category.Id,
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
                        Price = oi.Price
                    }).ToList()
                })
                .FirstOrDefault();

            if (menu == null)
                return NotFound();

            return Ok(menu);
        }

        [HttpPost]
        [Authorize(Roles = "AdminAssistant,Admin")]
        public async Task<IActionResult> AddMenu([FromForm] AddMenuDTO Menu)
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
                    return BadRequest();
                }
                

                var mappedMenu = mapping.Map<Menu>(Menu);
                mappedMenu.IsDeleted = false;
                mappedMenu.CreatedAt = DateTime.UtcNow;
                mappedMenu.IsAvailable = true;
                unitOfWork.Generic<Menu>().Add(mappedMenu);
                await unitOfWork.Complete();
                return Ok("Menu added successfully");
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
                return NotFound("This Menu Not Found");
            }

            var finalCategoryId = Menu.CategoryId ?? check.CategoryId;
            var finalMenuName = Menu.MenuName ?? check.MenuName;
            var finalDescription = Menu.Description ?? check.Description;

            check.CategoryId = finalCategoryId;
            check.MenuName = finalMenuName;
            check.Description = finalDescription;

            check.UpdatedAt = DateTime.UtcNow;

            await unitOfWork.Complete();
            return Ok("Menu updated successfully");
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
                    return NotFound();
                }
                check.IsDeleted = true;
                await unitOfWork.Complete();
                return Ok("Menu Deleted Successfully");
            }
        }
        [HttpPut("Restore")]
        [Authorize(Roles = "AdminAssistant,Admin")]
        public async Task<IActionResult> Restore(int id)
        {
            var check = unitOfWork.Generic<Menu>().GetById(id);
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
