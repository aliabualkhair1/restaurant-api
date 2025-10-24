using AutoMapper;
using BLL.Interfaces;
using DAL.DTOs.Models;
using DAL.DTOs.Models.Add;
using DAL.DTOs.Models.Update;
using DAL.DTOs.SetUp;
using DAL.Entities.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Restaurant.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "ValidToken")]

    public class CategoryController : ControllerBase
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapping;

        public CategoryController(IUnitOfWork UnitOfWork,IMapper Mapping)
        {
            unitOfWork = UnitOfWork;
            mapping = Mapping;
        }
        [HttpGet]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public IActionResult GetAllCategories()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }
            else
            {
                var Category = unitOfWork.Generic<Category>().GetAll().Where(c=>c.IsDeleted==false).Select(cat => new SetCategory
                {
                    Id=cat.Id,
                    Name = cat.Name,
                    Menu = cat.Menu.Select(menu => new SetMenu
                    {
                        Name=menu.MenuName,
                        Description=menu.Description,
                        CategoryId=menu.CategoryId,
                        IsAvailable=menu.IsAvailable,
                        MenuItems=menu.MenuItems.Select(MI=>new SetMenuItems
                        {
                            MenuName=MI.Menu.MenuName,
                            ItemName=MI.ItemName,
                            ItemImage=MI.ItemImage,
                            Quantity =MI.Quantity,
                            Price=MI.Price

                        }).ToList()
                    }).ToList()


                }).ToList();
                return Ok(Category);
            }
        }
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin,Staff,Customer,AdminAssistant")]
        public IActionResult GetCategoryById(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            var category = unitOfWork.Generic<Category>()
                .GetAll()
                .Include(c => c.Menu)
                    .ThenInclude(m => m.MenuItems)
                .Where(c => c.Id == id &&c.IsDeleted==false)
                .Select(cat => new SetCategory
                {
                    Id=cat.Id,
                    Name = cat.Name,
                    Menu = cat.Menu.Select(menu => new SetMenu
                    {
                        Name = menu.MenuName,
                        Description = menu.Description,
                        CategoryId = menu.CategoryId,
                        IsAvailable = menu.IsAvailable,
                        MenuItems = menu.MenuItems.Select(mi => new SetMenuItems
                        {
                            MenuName = mi.Menu.MenuName,
                            ItemName = mi.ItemName,
                            ItemImage = mi.ItemImage,
                            Quantity = mi.Quantity,
                            Price = mi.Price
                        }).ToList()
                    }).ToList()
                })
                .FirstOrDefault();

            if (category == null)
                return NotFound();

            return Ok(category);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> CreateCategory([FromBody] AddCategoryDTO Category)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }
            else
            {
                if (Category == null)
                {
                    return BadRequest();
                }
                var mappedCategory = mapping.Map<Category>(Category);
                mappedCategory.IsDeleted = false;
                unitOfWork.Generic<Category>().Add(mappedCategory);
                await unitOfWork.Complete();
                return Ok("Category add successfully");
            }
        }
        [HttpPatch("{id:int}")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> UpdateCategory(UpdateCategoryDTO Category, int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            var check = unitOfWork.Generic<Category>().GetById(id);

            if (check == null || check.IsDeleted == true)
            {
                return NotFound("This Category Not Found");
            }

            var finalName = Category.Name ?? check.Name;

            check.Name = finalName;

            await unitOfWork.Complete();
            return Ok("Category updated successfully");
        }
        [HttpPut("SoftDelete")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }
            else
            {
                var check = unitOfWork.Generic<Category>().GetById(id);
                if (check == null)
                {
                    return NotFound();
                }
                check.IsDeleted = true;
                await unitOfWork.Complete();
                return Ok("Category Deleted Successfully");
            }
        }
        [HttpPut("Restore")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async  Task<IActionResult> Restore(int id)
        {
            var check=unitOfWork.Generic<Category>().GetById(id);
            if(check == null)
            {
                return NotFound("NotFound");
            }
            check.IsDeleted=false;
            await unitOfWork.Complete();
            return Ok("Restord done");
        }
    }
}
