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

        [EnableRateLimiting("Fixed")]
        [HttpPost("Register")]
        public async Task<IActionResult> Register(Register reg)
        {
            var checkuser = await user.Users.AnyAsync(u => u.PhoneNumber == reg.PhoneNumber);
            if (checkuser)
            {
                return BadRequest("رقم الهاتف موجود بالفعل، يرجى اختيار رقم آخر.");
            }
            var usercheck = await user.Users.AnyAsync(u => u.NationalId == reg.NationalId);
            if (usercheck)
            {
                return BadRequest("الرقم القومي موجود بالفعل، يرجى اختيار رقم آخر.");
            }
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
                ModelState.AddModelError(reg.FirstName, "يجب أن يتراوح طول الاسم الأول بين 3 و 12 حرفًا.");
                return BadRequest(ModelState);
            }
            if (reg.LastName.Length < 3 || reg.LastName.Length > 12)
            {
                ModelState.AddModelError(reg.LastName, "يجب أن يتراوح طول الاسم الأخير بين 3 و 12 حرفًا.");
                return BadRequest(ModelState);
            }
            if (reg.PhoneNumber.Length != 11)
            {
                ModelState.AddModelError(reg.PhoneNumber, "يجب أن يتكون رقم الهاتف من 11 رقمًا.");
                return BadRequest(ModelState);
            }
            if (reg.NationalId.Length != 14)
            {
                ModelState.AddModelError(reg.NationalId, "يجب أن يتكون الرقم القومي من 14 رقمًا.");
                return BadRequest(ModelState);
            }
            if (reg.Username.Length < 6 || reg.Username.Length > 20)
            {
                ModelState.AddModelError(reg.Username, "يجب أن يتراوح طول اسم المستخدم بين 6 و 20 حرفًا.");
                return BadRequest(ModelState);
            }
            if (reg.Password.Length < 6 || reg.Password.Length > 20)
            {
                ModelState.AddModelError(reg.Password, "يجب أن تتراوح كلمة المرور بين 6 و 20 حرفًا.");
                return BadRequest(ModelState);
            }

            var result = await user.CreateAsync(_user, reg.Password);
            if (result.Succeeded)
            {
                await user.AddToRoleAsync(_user, "Customer");
                return Ok("تم التسجيل بنجاح.");
            }

            foreach (var err in result.Errors)
            {
                ModelState.AddModelError("", err.Description);
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
                    if (User.IsDeleted == false)
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
                                claims.Add(new Claim("role", role));

                            var SK = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(confg["JWT:Key"]));
                            var SC = new SigningCredentials(SK, SecurityAlgorithms.HmacSha256);

                            var token = new JwtSecurityToken(
                                claims: claims,
                                issuer: confg["JWT:Issuer"],
                                audience: confg["JWT:Audience"],
                                signingCredentials: SC,
                                expires: DateTime.UtcNow.AddHours(1)
                            );

                            var expiredTokens = await db.Set<RefreshToken>()
                                .Where(rt => rt.UserId == User.Id && rt.ExpireDate <= DateTime.UtcNow)
                                .ToListAsync();
                            if (expiredTokens.Any())
                            {
                                db.Set<RefreshToken>().RemoveRange(expiredTokens);
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
                            return BadRequest("البريد الإلكتروني أو كلمة المرور غير صالحة.");
                        }
                    }
                    else
                    {
                        return StatusCode(410, "تم حذف حسابك.");
                    }
                }

                return BadRequest("بيانات تسجيل الدخول غير صالحة، يرجى المحاولة مرة أخرى.");
            }

            return BadRequest(ModelState);
        }

        [EnableRateLimiting("Fixed")]
        [HttpPut("RestPassword")]
        [Authorize(Policy = "ValidToken")]
        public async Task<IActionResult> ResetPassword(ForgetandChangPassword FACP)
        {
            if (ModelState.IsValid)
            {
                var _user = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var check = await user.FindByIdAsync(_user);

                if (check == null || check.IsDeleted == true)
                {
                    return BadRequest("هذا البريد الإلكتروني غير مسجل.");
                }

                if (FACP.NewPassword.Length < 6 || FACP.NewPassword.Length > 20)
                {
                    ModelState.AddModelError(FACP.NewPassword, "يجب أن تتراوح كلمة المرور الجديدة بين 6 و 20 حرفًا.");
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

                return Ok("تم تغيير كلمة المرور بنجاح.");
            }

            return BadRequest("البيانات غير صحيحة، يرجى المحاولة مرة أخرى.");
        }

        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshToken(Token token)
        {
            var refreshtoken = token.token;

            if (string.IsNullOrEmpty(refreshtoken))
            {
                return BadRequest("مفتاح التحديث مطلوب.");
            }

            var existingToken = await db.Set<RefreshToken>()
                .FirstOrDefaultAsync(rt => rt.Token == refreshtoken && !rt.IsRevoked);

            if (existingToken == null || existingToken.ExpireDate <= DateTime.UtcNow)
            {
                if (existingToken != null && existingToken.ExpireDate <= DateTime.UtcNow)
                {
                    existingToken.IsRevoked = true;
                    await db.SaveChangesAsync();
                }
                return BadRequest("مفتاح تحديث غير صالح أو منتهي الصلاحية.");
            }

            var User = await user.FindByIdAsync(existingToken.UserId);
            if (User == null)
            {
                return NotFound("المستخدم غير موجود.");
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
                claims.Add(new Claim("role", role));
            }

            var SK = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(confg["JWT:Key"]));
            var SC = new SigningCredentials(SK, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new JwtSecurityToken(
                claims: claims,
                issuer: confg["JWT:Issuer"],
                audience: confg["JWT:Audience"],
                signingCredentials: SC,
                expires: DateTime.UtcNow.AddHours(1)
            );

            existingToken.IsRevoked = true;
            await db.SaveChangesAsync();

            var retoken = new GenerateRefreshToken();
            var gennewreftoken = retoken.GeneraterefreshToken();

            var newRefresh = new RefreshToken
            {
                Token = gennewreftoken,
                UserId = User.Id,
                ExpireDate = DateTime.UtcNow.AddDays(10),
                IsRevoked = false
            };

            await db.Set<RefreshToken>().AddAsync(newRefresh);
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
