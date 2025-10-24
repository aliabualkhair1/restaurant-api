using BLL.Repositories;
using DAL.DTOs.Auth;
using DAL.Entities.AppUser;
using DAL.Entities.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Restaurant.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserInfoController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> user;
        private readonly SignInManager<ApplicationUser> sign;

        public UserInfoController(UserManager<ApplicationUser>User)
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
                return Unauthorized();
            }
            var UserDetails = await user.FindByIdAsync(userId);
            if (UserDetails == null)
            {
                return NotFound("No Data Founded");
            }
            var roles = await user.GetRolesAsync(UserDetails);
            var result = new
            {
                UserId=UserDetails.Id,
                UserName = UserDetails.UserName,
                Email = UserDetails.Email,
                Roles = roles
            };
            return Ok(result);
        }
        [HttpPatch("UpdateUserDetails")]
        [Authorize(Roles = "Admin,Staff,Customer,AdminAssistant")]
        [Authorize(Policy = "ValidToken")]
        public async Task<IActionResult> UpdateUserDetails([FromBody]UpdatePersonalDetails _user)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();
            var userDetails = await user.FindByIdAsync(userId);
            if (userDetails == null)
                return NotFound("User not found.");
            if (!string.IsNullOrWhiteSpace(_user.NewPassword))
            {
                if (string.IsNullOrWhiteSpace(_user.CurrentPassword))
                    return BadRequest("Current password is required to set a new password.");
                var passwordResult = await user.ChangePasswordAsync(userDetails, _user.CurrentPassword, _user.NewPassword);
                if (!passwordResult.Succeeded)
                    return BadRequest(passwordResult.Errors);
            }
            if (!string.IsNullOrWhiteSpace(_user.Username))
                userDetails.UserName = _user.Username;
            var emailaddress = new EmailAddressAttribute();
            if (!string.IsNullOrWhiteSpace(_user.Email)&&emailaddress.IsValid(_user.Email))
                userDetails.Email = _user.Email;
            userDetails.TokenVersion++;
            await user.UpdateSecurityStampAsync(userDetails);
            var result = await user.UpdateAsync(userDetails);
            if (!result.Succeeded)
                return BadRequest(result.Errors);
            return Ok("User details updated successfully.");
        }

        [HttpPut("SoftDeleteAccount")]
        [Authorize(Roles = "Admin,Customer,Staff,AdminAssistant")]
        [Authorize(Policy = "ValidToken")]
        public async Task<IActionResult> DeleteAccount(DeleteAccount delete)
        {
            var check = await user.FindByNameAsync(delete.username);
            if (check != null&&check.IsDeleted==false)
            {
                check.IsDeleted = true;
                await user.UpdateAsync(check);
                return Ok("Your account deleted succcessfully");
            }
            else
            {
                return BadRequest("Invalid data  or account deleted before");
            }

        }
        [HttpPut("Restore")]
        [EnableRateLimiting("Fixed")]
        [Authorize(Roles = "Admin,Customer,Staff,AdminAssistant")]
        [Authorize(Policy = "ValidToken")]
        public async Task<IActionResult> Restore(SignIn signin)
        {

            var check = await user.FindByNameAsync(signin.Username);
            if (check != null&&check.IsDeleted==true)
            {
               var password=await user.CheckPasswordAsync(check,signin.Password);
                if (password)
                {
                    check.IsDeleted = false;
                    check.TokenVersion++;
                    await user.UpdateAsync(check);
                    return Ok("Restord done");
                }
                else
                {
                return Unauthorized("Data Invalid");
                }
            }
            else
            {
                    return BadRequest("Your personal details was wrong please try again or account not deleted yet");
            }
   
        }
    }
}
