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

    public class CategoryController : ControllerBase
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapping;

        public CategoryController(IUnitOfWork UnitOfWork, IMapper Mapping)
        {
            unitOfWork = UnitOfWork;
            mapping = Mapping;
        }

        [HttpGet]
        public IActionResult GetAllCategories()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
          
 
                var category = unitOfWork.Generic<Category>().GetAll().Where(cat => cat.IsDeleted == false).Select(cat => new SetCategory
                {
                    Id = cat.Id,
                    Name = cat.Name,
                    IsDeleted = cat.IsDeleted

                });

                if (!category.Any())
                {
                    return NotFound("لم يتم إضافة أي فئة حتى الآن.");
                }

                return Ok(category);
            }
        

        [HttpGet("GetByCategoryName")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public IActionResult GetByCategoryName(string name)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized("غير مصرح لك بالوصول.");
            }
            var category = unitOfWork.Generic<Category>().GetAll().Where(cat => cat.IsDeleted == false && cat.Name == name).Select(cat => new SetCategory
            {
                Id = cat.Id,
                Name = cat.Name,
                IsDeleted = cat.IsDeleted

            });

            if (!category.Any())
            {
                return NotFound("الفئة غير موجودة.");
            }

            return Ok(category);
        }

        [HttpGet("GetDeletedCategoryByName")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public IActionResult GetDeletedCategoryByName(string name)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized("غير مصرح لك بالوصول.");
            }
            var category = unitOfWork.Generic<Category>().GetAll().Where(cat => cat.IsDeleted == true && cat.Name == name).Select(cat => new SetCategory
            {
                Id = cat.Id,
                Name = cat.Name,
                IsDeleted = cat.IsDeleted

            });

            if (!category.Any())
            {
                return NotFound("الفئة غير موجودة.");
            }

            return Ok(category);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> CreateCategory([FromBody] AddCategoryDTO Category)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized("غير مصرح لك بالوصول.");
            }
            else
            {
                if (Category == null)
                {
                    return BadRequest("البيانات المرسلة فارغة أو غير صحيحة.");
                }
                var mappedCategory = mapping.Map<Category>(Category);
                mappedCategory.IsDeleted = false;
                var existcategory = unitOfWork.Generic<Category>().GetAll().Where(c => c.Name == Category.Name);
                if (!existcategory.Any())
                {
                    mappedCategory.Name = Category.Name;
                }
                else
                {
                    return BadRequest("الفئة موجودة بالفعل، يرجى اختيار اسم آخر.");
                }
                unitOfWork.Generic<Category>().Add(mappedCategory);
                await unitOfWork.Complete();
                return Ok("تمت إضافة الفئة بنجاح.");
            }
        }

        [HttpPatch("{id:int}")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> UpdateCategory(UpdateCategoryDTO category, int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized("غير مصرح لك بالوصول.");
            }

            var check = unitOfWork.Generic<Category>().GetById(id);

            if (check == null || check.IsDeleted == true)
            {
                return NotFound("هذه الفئة غير موجودة.");
            }

            var finalName = category.Name ?? check.Name;
            var existcategory = unitOfWork.Generic<Category>().GetAll().Where(c => c.Name == finalName);
            if (existcategory.Any())
            {
                return BadRequest("هذه الفئة موجودة مسبقا من فضلك اختر اسم اخر");
            }
            check.Name = finalName;
            await unitOfWork.Complete();
            return Ok("تم تحديث الفئة بنجاح.");
        }

        [HttpPut("SoftDelete")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized("غير مصرح لك بالوصول.");
            }
            else
            {
                var check = unitOfWork.Generic<Category>().GetById(id);
                if (check == null)
                {
                    return NotFound("الفئة غير موجودة.");
                }
                else if (check.IsDeleted == true)
                {
                    return BadRequest("تم حذف الفئة مسبقاً.");
                }
                check.IsDeleted = true;
                await unitOfWork.Complete();
                return Ok("تم حذف الفئة بنجاح.");
            }
        }

        [HttpGet("GetAllDeletedCategories")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public IActionResult GetAllDeletedCategories()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized("غير مصرح لك بالوصول.");
            }
            else
            {
                var category = unitOfWork.Generic<Category>().GetAll().Where(cat => cat.IsDeleted == true).Select(cat => new SetCategory
                {
                    Id = cat.Id,
                    Name = cat.Name,
                    IsDeleted = cat.IsDeleted

                });

                if (!category.Any())
                {
                    return Ok(new List<SetCategory>());

                }

                return Ok(category);
            }
        }

        [HttpPut("Restore")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> Restore(int id)
        {
            var check = unitOfWork.Generic<Category>().GetById(id);
            if (check == null)
            {
                return NotFound("الفئة غير موجودة.");
            }
            else if (check.IsDeleted == false)
            {
                return BadRequest("الفئة غير محذوفة بعد.");
            }
            check.IsDeleted = false;
            await unitOfWork.Complete();
            return Ok("تمت الاستعادة بنجاح.");
        }
    }
}