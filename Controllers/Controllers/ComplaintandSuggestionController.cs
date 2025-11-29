using AutoMapper;
using BLL.Interfaces;
using BLL.Repositories;
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
    public class ComplaintandSuggestionController : ControllerBase
    {
        private readonly IUnitOfWork unitofWork;
        private readonly IMapper mapping;

        public ComplaintandSuggestionController(IUnitOfWork UnitofWork, IMapper Mapping)
        {
            unitofWork = UnitofWork;
            mapping = Mapping;
        }

        [HttpGet("GetAllComplaintandSuggestion")]
        [Authorize(Roles = "Admin,Customer,Staff,AdminAssistant")]
        public IActionResult GetAllComplaintandSuggestion()
        {
            var userid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userid != null)
            {
                var Getall = unitofWork.Generic<ComplaintandSuggestion>().GetAll().Where(cs => cs.IsDeleted == false && cs.UserId == userid).Include(cs => cs.User).Select(cs => new SetComplaintandSuggestion
                {
                    Id = cs.Id,
                    UserId = cs.UserId,
                    Username = cs.User.UserName,
                    Problemandsolving = cs.Problemandsolving,
                    Date = cs.Date
                });
                return Ok(Getall);
            }
            return BadRequest("غير مسموح لك بالدخول");
        }

        [HttpGet("GetAllDeletedComplaintandSuggestion")]
        [Authorize(Roles = "Admin,Customer,Staff,AdminAssistant")]
        public IActionResult GetAllDeletedComplaintandSuggestion()
        {
            var userid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userid != null)
            {
                var Getall = unitofWork.Generic<ComplaintandSuggestion>().GetAll().Where(cs => cs.IsDeleted == true && cs.UserId == userid).Include(cs => cs.User).Select(cs => new SetComplaintandSuggestion
                {
                    Id = cs.Id,
                    UserId = cs.UserId,
                    Username = cs.User.UserName,
                    Problemandsolving = cs.Problemandsolving,
                    Date = cs.Date
                });
                return Ok(Getall);
            }
            return BadRequest("غير مسموح لك بالدخول");
        }

        [HttpPost("CreateComplaintandSuggestion")]
        [Authorize(Roles = "Admin,Customer,Staff,AdminAssistant")]
        public async Task<IActionResult> CreateComplaintandSuggestion([FromBody] AddComplaintandSuggestionDTO CS)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }
            else
            {

                var mapped = mapping.Map<ComplaintandSuggestion>(CS);
                mapped.UserId = userId;
                mapped.Date = DateTime.Now;
                mapped.IsDeleted = false;
                unitofWork.Generic<ComplaintandSuggestion>().Add(mapped);
                await unitofWork.Complete();
                return Ok("تمت الإضافة بنجاح.");
            }

        }

        [HttpPatch("UpdateComplaintandSuggestion/{id:int}")]
        [Authorize(Roles = "Admin,Customer,Staff,AdminAssistant")]
        public async Task<IActionResult> UpdateComplaintandSuggestion(int id, [FromBody] UpdateComplaintandSuggestionDTO CS)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            var cs = unitofWork.Generic<ComplaintandSuggestion>().GetById(id);

            if (cs == null)
                return NotFound("لم يتم العثور على الشكوى أو الاقتراح.");

            if (cs.UserId != userId)
                return Unauthorized("غير مصرح لك بتحديث هذا العنصر.");

            var finalProblemandsolving = CS.Problemandsolving ?? cs.Problemandsolving;

            cs.Problemandsolving = finalProblemandsolving;

            cs.Date = DateTime.Now;

            unitofWork.Generic<ComplaintandSuggestion>().Update(cs);
            await unitofWork.Complete();

            return Ok("تم التحديث بنجاح.");
        }

        [HttpPut("DeleteComplaintandSuggestion")]
        [Authorize(Roles = "Admin,Customer,Staff,AdminAssistant")]
        public async Task<IActionResult> DeleteComplaintandSuggestion(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }
            else
            {
                var check = unitofWork.Generic<ComplaintandSuggestion>().GetById(id);
                if (check != null && check.UserId == userId)
                {
                    check.IsDeleted = true;
                    await unitofWork.Complete();
                    return Ok("تم الحذف بنجاح.");
                }
                return BadRequest("لا يمكنك الحذف .");
            }
        }

        [HttpPut("Restore")]
        [Authorize(Roles = "Admin,Customer,Staff,AdminAssistant")]
        public async Task<IActionResult> Restore(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            var check = unitofWork.Generic<ComplaintandSuggestion>().GetById(id);
            if (check != null && check.UserId == userId)
            {
                check.IsDeleted = false;
                await unitofWork.Complete();
                return Ok("تمت الاستعادة بنجاح.");
            }

            return BadRequest("لا يمكنك الاستعادة.");
        }

    }
}