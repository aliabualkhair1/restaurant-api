using BLL.Interfaces;
using DAL.DbContext;
using DAL.DTOs.Auth;
using DAL.Entities.AppUser;
using DAL.Entities.Models;
using DAL.Entities.RefreshToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Restaurant.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> user;
        private readonly SignInManager<ApplicationUser> op;
        private readonly IConfiguration confg;
        private readonly RestaurantDB db;
        private readonly IUnitOfWork unitOfWork;

        public AuthenticationController(UserManager<ApplicationUser> User, IConfiguration Confg, RestaurantDB Db)
        {
            user = User;
            confg = Confg;
            db = Db;
        }
        [HttpPost("Register")]
        public async Task<IActionResult> Register(Register reg)
        {
            var _user = new ApplicationUser
            {
                FirstName = reg.FirstName,
                LastName = reg.LastName,
                PhoneNumber = reg.PhoneNumber,
                NationalId = reg.NationalId,
                UserName = reg.Username,
                Email = reg.Email,
            };
            _user.IsDeleted = false;
            if (reg.FirstName.Length < 3 || reg.FirstName.Length > 12)
            {
                ModelState.AddModelError(reg.FirstName, "First Name must be between 3 and 12 characters");
                return BadRequest(ModelState);
            }
            if (reg.LastName.Length < 3 || reg.LastName.Length > 12)
            {
                ModelState.AddModelError(reg.LastName, "Last Name must be between 3 and 12 characters");
                return BadRequest(ModelState);
            }
            if (reg.PhoneNumber.Length!=11)
            {
                ModelState.AddModelError(reg.PhoneNumber, "Phone Number must be 11 Number");
                return BadRequest(ModelState);
            }
            if (reg.NationalId.Length!=14)
            {
                ModelState.AddModelError(reg.NationalId, "National Id must be 14 Number");
                return BadRequest(ModelState);
            }
            if (reg.Username.Length < 3 || reg.Username.Length > 20 )
            {
                ModelState.AddModelError(reg.Username, "Username must be between 3 and 20 characters");
                return BadRequest(ModelState);
            }
            if (reg.Password.Length < 6 || reg.Password.Length > 20)
            {
                ModelState.AddModelError(reg.Password, "Password must be between 6 and 20 characters");
                return BadRequest(ModelState);
            }
            var result = await user.CreateAsync(_user, reg.Password);
            if (result.Succeeded)
            {
                await user.AddToRoleAsync(_user, "Customer");
                return Ok("Your Registration is Done");
            }
            foreach (var err in result.Errors)
            {
                ModelState.AddModelError("",err.Description);
            }
            return BadRequest(ModelState);
        }
        [EnableRateLimiting("Fixed")]
        [HttpPost("LogIn")]
        public async Task<IActionResult> SignIn(SignIn signIn)
        {
            if (ModelState.IsValid)
            {
                var User = await user.FindByEmailAsync(signIn.Email);
                var RefreshToken = new GenerateRefreshToken();
                var Token = RefreshToken.GeneraterefreshToken();

                if (User != null)
                {
                    var pass = await user.CheckPasswordAsync(User, signIn.Password);
                    if (pass)
                    {
                        var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, User.Id.ToString()),
                    new Claim(ClaimTypes.Email, signIn.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim("token_version", User.TokenVersion.ToString())
                };

                        var roles = await user.GetRolesAsync(User);
                        foreach (var role in roles)
                            claims.Add(new Claim(ClaimTypes.Role, role));

                        var SK = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(confg["JWT:Key"]));
                        var SC = new SigningCredentials(SK, SecurityAlgorithms.HmacSha256);

                        var token = new JwtSecurityToken(
                            claims: claims,
                            issuer: confg["JWT:Issuer"],
                            audience: confg["JWT:Audience"],
                            signingCredentials: SC,
                            expires: DateTime.Now.AddHours(1)
                        );

                        var oldRefreshToken = await db.Set<RefreshToken>()
                            .FirstOrDefaultAsync(rt => rt.UserId == User.Id && !rt.IsRevoked);

                        if (oldRefreshToken != null)
                        {
                            oldRefreshToken.IsRevoked = true;
                            await db.SaveChangesAsync();
                        }

                        var newRefresh = new RefreshToken
                        {
                            Token = Token,
                            UserId = User.Id,
                            ExpireDate = DateTime.UtcNow.AddDays(10),
                            IsRevoked = false
                        };

                        await db.AddAsync(newRefresh);
                        await db.SaveChangesAsync();

                        var _token = new
                        {
                            token = new JwtSecurityTokenHandler().WriteToken(token),
                            expire = token.ValidTo,
                            refreshtoken = newRefresh.Token
                        };
                        return Ok(_token);
                    }
                    else
                    {
                        return Unauthorized("Username Or Password Is Invalid ");
                    }
                }
                return BadRequest(ModelState);
            }

            return BadRequest(ModelState);
        }
        [HttpPost("RestPassword")]
        [Authorize(Policy = "ValidToken")]
        public async Task<IActionResult> ResetPassword(ForgetandChangPassword FACP)
        {
            if (ModelState.IsValid)
            {
                var check = await user.FindByEmailAsync(FACP.Email);
                if (check == null)
                {
                    return BadRequest("This email not registered");
                }
                if (FACP.NewPassword.Length < 6 || FACP.NewPassword.Length > 20)
                {
                    ModelState.AddModelError(FACP.NewPassword, "New Password must be between 6 and 20 characters");
                    return BadRequest(ModelState);
                }
                var change = await user.ChangePasswordAsync(check, FACP.CurrentPassword, FACP.NewPassword);
                if (!change.Succeeded)
                {
                    return BadRequest(change.Errors);
                }
                var token = await db.Set<RefreshToken>().Where(rt => rt.UserId == check.Id && !rt.IsRevoked).ToListAsync();
                if (token.Any())
                {
                    foreach (var _token in token)
                    {
                        _token.IsRevoked = true;
                    }
                    await db.SaveChangesAsync();
                }
                check.TokenVersion++;
                db.Set<ApplicationUser>().Update(check);
                await db.SaveChangesAsync();
                return Ok("Your password changed successfully");
            }
            return BadRequest(ModelState);
        }
        [HttpPost("LogOut")]
        [Authorize(Policy = "ValidToken")]
        public async Task<IActionResult> LogOut()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }
            var token=await db.Set<RefreshToken>().Where(rt => rt.UserId == userId && !rt.IsRevoked).ToListAsync();
            if (token.Any())
            {
                foreach (var _token in token)
                {
                    _token.IsRevoked = true;
                }
                var userDetails = await user.FindByIdAsync(userId);
                userDetails.TokenVersion++;
                db.Set<ApplicationUser>().Update(userDetails);
                await db.SaveChangesAsync();
            }
                return Ok("LogOut Done");
        }

        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshToken(Token token)
        {
            var refreshtoken = token.token;

            if (string.IsNullOrEmpty(refreshtoken))
            {
                return BadRequest("Refresh Token is required");
            }
            var existingToken = db.Set<RefreshToken>().FirstOrDefault(rt => rt.Token == refreshtoken && !rt.IsRevoked);
            if (existingToken == null || existingToken.ExpireDate <= DateTime.Now)
            {
                return BadRequest("Invalid or expired refresh token");
            }
            var User = await user.FindByIdAsync(existingToken.UserId);
            if (User == null)
            {
                return NotFound("User not found");
            }
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, User.Id.ToString()),
                new Claim(ClaimTypes.Name, User.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("token_version", User.TokenVersion.ToString())

            };
            var roles = await user.GetRolesAsync(User);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            var SK = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(confg["JWT:Key"]));
            var SC = new SigningCredentials(SK, SecurityAlgorithms.HmacSha256); 
            var tokenDescriptor = new JwtSecurityToken(
                claims: claims,
                issuer: confg["JWT:Issuer"],
                audience: confg["JWT:Audience"],
                signingCredentials: SC,
                expires: DateTime.Now.AddHours(1)
            );
            existingToken.IsRevoked = true;
            var retoken = new GenerateRefreshToken();
            var gennewreftoken = retoken.GeneraterefreshToken();
                db.Set<RefreshToken>().Add(new RefreshToken
                {
                    Token = gennewreftoken,
                    UserId = User.Id,
                    ExpireDate = DateTime.UtcNow.AddDays(10),
                    IsRevoked = false
                });
            await db.SaveChangesAsync();


            var GenToken = new
            {
                token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor),
                expire = tokenDescriptor.ValidTo,
                refreshtoken = gennewreftoken
            };
            return Ok(GenToken);
        }
    }
}
