using DAL.Entities.AppUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Restaurant.AuthorizationPipline
{
    public class TokenHandler : AuthorizationHandler<AuthorizationRequirment>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContext;

        public TokenHandler(UserManager<ApplicationUser> userManager, IHttpContextAccessor httpContext)
        {
            _userManager = userManager;
            _httpContext = httpContext;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            AuthorizationRequirment requirement)
        {
            var http = _httpContext.HttpContext;

            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var tokenVersionClaim = context.User.FindFirst("token_version")?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tokenVersionClaim))
            {
                context.Fail();
                return;
            }

            if (!int.TryParse(tokenVersionClaim, out int tokenVersion))
            {
                context.Fail();
                http.Response.Headers.Add("auth", "InvalidToken");
                return;
            }

            var dbUser = await _userManager.FindByIdAsync(userId);

            if (dbUser == null||dbUser.IsDeleted==true)
            {
                context.Fail();
                http.Response.Headers.Add("auth", "UserNotFound");
                return;
            }

            if (dbUser.TokenVersion != tokenVersion)
            {
                context.Fail();
                http.Response.Headers.Add("auth", "InvalidToken");
                return;
            }
            context.Succeed(requirement);
        }
    }
}
