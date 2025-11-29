using BLL.Repositories;
using DAL.DTOs.Auth;
using DAL.Entities.AppUser;
using DAL.Entities.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Restaurant.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserInfoController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> user;

        public UserInfoController(UserManager<ApplicationUser> User)
        {
            user = User;
        }

        [Authorize(Roles = "Admin,Staff,Customer,AdminAssistant")]
        [HttpGet("UserDetails")]
        public async Task<IActionResult> GetUserDetails()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized("غير مصرح لك بالوصول.");
            }
            var UserDetails = await user.FindByIdAsync(userId);
            if (UserDetails == null || UserDetails.IsDeleted == true)
            {
                return NotFound("لم يتم العثور على بيانات.");
            }
            var roles = await user.GetRolesAsync(UserDetails);
            var result = new
            {
                UserId = UserDetails.Id,
                UserName = UserDetails.UserName,
                FirstName = UserDetails.FirstName,
                LastName = UserDetails.LastName,
                Email = UserDetails.Email,
                NationalId = UserDetails.NationalId,
                PhoneNumber = UserDetails.PhoneNumber,
                Roles = roles
            };
            return Ok(result);
        }

        [HttpPatch("UpdateUserDetails")]
        [Authorize(Roles = "Admin,Staff,Customer,AdminAssistant")]
        [Authorize(Policy = "ValidToken")]
        [EnableRateLimiting("Fixed")]
        public async Task<IActionResult> UpdateUserDetails([FromBody] UpdatePersonalDetails _user)
        {
            if (!ModelState.IsValid)
                return BadRequest("بيانات غير صالحة. يرجى التحقق من المدخلات.");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized("غير مصرح لك بالوصول.");

            var userDetails = await user.FindByIdAsync(userId);
            if (userDetails == null || userDetails.IsDeleted == true)
                return NotFound("لم يتم العثور على المستخدم.");

            var emailValidator = new EmailAddressAttribute();

            if (_user.FirstName != null)
            {
                if (string.IsNullOrWhiteSpace(_user.FirstName) || _user.FirstName.Length < 3 || _user.FirstName.Length > 12)
                    return BadRequest("يجب أن يتراوح الاسم الأول بين 3 و 12 حرفًا.");
                userDetails.FirstName = _user.FirstName;
            }

            if (_user.LastName != null)
            {
                if (string.IsNullOrWhiteSpace(_user.LastName) || _user.LastName.Length < 3 || _user.LastName.Length > 12)
                    return BadRequest("يجب أن يتراوح الاسم الأخير بين 3 و 12 حرفًا.");
                userDetails.LastName = _user.LastName;
            }
            if (_user.Email != null)
            {
                if (string.IsNullOrWhiteSpace(_user.Email) || !emailValidator.IsValid(_user.Email))
                    return BadRequest("عنوان البريد الإلكتروني غير صالح.");
                userDetails.Email = _user.Email;
            }

            if (_user.PhoneNumber != null)
            {
                if (string.IsNullOrWhiteSpace(_user.PhoneNumber) || _user.PhoneNumber.Length != 11)
                    return BadRequest("يجب أن يتكون رقم الهاتف من 11 رقمًا بالضبط.");
                userDetails.PhoneNumber = _user.PhoneNumber;
            }
            var Users = user.Users.ToList();
            foreach(var user in Users)
            {
                if (user.Email == _user.Email)
                {
                    return BadRequest("هذا الايميل موجود مسبقا من فضلك اختر إيميل آخر");
                }
                else if (user.PhoneNumber == _user.PhoneNumber)
                {
                    return BadRequest("هذا الرقم موجود مسبقا من فضلك اختر رقم آخر");
                }
            }
            userDetails.TokenVersion++;
            await user.UpdateSecurityStampAsync(userDetails);
            var result = await user.UpdateAsync(userDetails);
            if (!result.Succeeded)
            return BadRequest(string.Join(" | ", result.Errors.Select(e => e.Description)));

            return Ok("تم تحديث بياناتك بنجاح.");
        }

        [HttpPut("SoftDeleteAccount")]
        [Authorize(Roles = "Admin,Customer,Staff,AdminAssistant")]
        [Authorize(Policy = "ValidToken")]
        public async Task<IActionResult> DeleteAccount()
        {
            var _user = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var check = await user.FindByIdAsync(_user);
            if (check != null && check.IsDeleted == false)
            {
                check.IsDeleted = true;
                await user.UpdateAsync(check);
                return Ok("تم حذف حسابك بنجاح.");
            }
            else
            {
                return BadRequest("بيانات غير صالحة أو تم حذف الحساب مسبقًا.");
            }
        }

        [HttpPut("Restore")]
        [EnableRateLimiting("Fixed")]
        public async Task<IActionResult> Restore(RestoreAccount restore)
        {
            var check = await user.FindByNameAsync(restore.Username);
            if (check != null && check.IsDeleted == true)
            {
                if (check.NationalId == restore.NationalId && check.Email == restore.Email)
                {
                    var password = await user.CheckPasswordAsync(check, restore.Password);
                    if (password)
                    {
                        check.IsDeleted = false;
                        await user.UpdateAsync(check);
                        return Ok("تمت استعادة الحساب بنجاح.");
                    }
                    else
                    {
                        return Unauthorized("بيانات غير صالحة. حاول مرة أخرى.");
                    }
                }
                return BadRequest("عذراً، لا يمكننا استعادة حسابك مرة أخرى.");
            }
            else
            {
                return BadRequest("لا يوجد حساب بهذا الاسم. يرجى اختيار اسم مستخدم صحيح آخر.");
            }
        }
        [HttpPut("CheckAccount")]
        [EnableRateLimiting("Fixed")]
        public async Task<IActionResult> CheckAccount(Forget_Password forget)
        {
            var check = await user.FindByNameAsync(forget.Username);

            if (check == null)
                return BadRequest("لا يوجد حساب بهذا الاسم. يرجى اختيار اسم مستخدم آخر.");

            if (check.IsDeleted == true)
                return BadRequest("عذراً، لا يمكننا المساعدة فى تغيير الباسورد فى الوقت الحالى  .");

            if (check.NationalId != forget.NationalId || check.Email != forget.Email)
                return BadRequest("بيانات غير صالحة. حاول مرة أخرى.");
            return Ok("تم التحقق من البيانات بنجاح. جاري تحويلك لصفحة تغيير كلمة المرور.");
        }

        [HttpPut("ResetPassword")]
        public async Task<IActionResult> ResetPassword(ResetPassword reset)
        {
            var check = await user.FindByNameAsync(reset.Username);
            if (check != null && check.IsDeleted == false)
            {
               
                    var token = await user.GeneratePasswordResetTokenAsync(check);
                    var result = await user.ResetPasswordAsync(check, token, reset.Password);
                    if (result.Succeeded)
                    {
                        return Ok("تم إعادة تعيين كلمة المرور بنجاح.");
                    }
                    else
                    {
                        return BadRequest("فشل في إعادة تعيين كلمة المرور. حاول مرة أخرى.");
                    }
            }
            else
            {
                return BadRequest("لا يوجد حساب بهذا الاسم. يرجى اختيار اسم مستخدم أخر .");
            }
        }
    }
}