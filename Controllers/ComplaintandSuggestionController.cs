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
    [Authorize(Policy = "ValidToken")]

    public class ComplaintandSuggestionController : ControllerBase
    {
        private readonly IUnitOfWork unitofWork;
        private readonly IMapper mapping;

        public ComplaintandSuggestionController(IUnitOfWork UnitofWork,IMapper Mapping)
        {
            unitofWork = UnitofWork;
            mapping = Mapping;
        }
        [HttpGet("GetAllComplaintandSuggestion")]
        [Authorize(Roles = "Admin,Customer,Staff,AdminAssistant")]
        public IActionResult GetAllComplaintandSuggestion()
        {
            var userid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userid!=null)
            {
                var Getall = unitofWork.Generic<ComplaintandSuggestion>().GetAll().Where(cs => cs.IsDeleted == false && cs.UserId == userid).Include(cs => cs.User).Select(cs => new SetComplaintandSuggestion
                {
                    Id=cs.Id,
                    UserId = cs.UserId,
                    Username = cs.User.UserName,
                    Problemandsolving=cs.Problemandsolving,
                    Date=cs.Date
                });
                return Ok(Getall);
            }
            return Unauthorized();
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
                mapped.Date = DateTime.UtcNow;
                mapped.IsDeleted = false;
                unitofWork.Generic<ComplaintandSuggestion>().Add(mapped);
                await unitofWork.Complete();
                return Ok("ComplaintandSuggestion Added Successfully");
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
                return NotFound("Complaint or Suggestion Not Found");

            if (cs.UserId != userId)
                return Unauthorized("You are not authorized to update this item.");

            var finalProblemandsolving = CS.Problemandsolving ?? cs.Problemandsolving;

            cs.Problemandsolving = finalProblemandsolving;

            cs.Date = DateTime.UtcNow;

            unitofWork.Generic<ComplaintandSuggestion>().Update(cs);
            await unitofWork.Complete();

            return Ok("Complaint or Suggestion Updated Successfully");
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
                if (check != null&&check.UserId==userId)
                {
                check.IsDeleted = true;
                await unitofWork.Complete();
                return Ok("DeleteComplaintandSuggestion Deleted Successfully");
            }
                return Unauthorized("You can not delete this ComplaintandSuggestion");
            }
        }
        [HttpPut("Restore")]
        [Authorize(Roles = "Admin,Customer,Staff,AdminAssistant")]
        public  IActionResult Restore(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }
            var check = unitofWork.Generic<ComplaintandSuggestion>().GetById(id);
            if (check != null&&check.UserId==userId)
            {
            check.IsDeleted = false;
            unitofWork.Complete();
            return Ok("Restord done");
            }
            return Unauthorized("You can not restore this ComplaintandSuggestion");
        }
    }
}
