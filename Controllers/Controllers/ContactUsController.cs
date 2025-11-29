using BLL.Interfaces;
using DAL.DTOs.Models.Add;
using DAL.Entities.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Restaurant.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactUsController : ControllerBase
    {
        private readonly IUnitOfWork unitOfWork;

        public ContactUsController(IUnitOfWork UnitOfWork)
        {
           unitOfWork = UnitOfWork;
        }
        [HttpPost("ContactUs")]
        public  async Task<IActionResult> AddContactUs(SetContactUs contact)
        {
            if (ModelState.IsValid)
            {
                var Add = new ContactUs
                {
                    FullName = contact.FullName,
                    Email = contact.Email,
                    Message = contact.Message,
                    DateOfSending = DateTime.Now,
                    IsDeleted = false
                };
                unitOfWork.Generic<ContactUs>().Add(Add);
                await unitOfWork.Complete();
                return Ok("تم الإرسال بنجاح");
            }
            return BadRequest("حدث خطأ ما برجاء المحاولة مرة أخرى");
        }
    }
 }

