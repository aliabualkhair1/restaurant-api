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

            if (http == null)
            {
                context.Fail();
                return;
            }
            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var tokenVersionClaim = context.User.FindFirst("token_version")?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tokenVersionClaim))
            {
                AddHeaderSafe(http, "auth", "InvalidToken");
                context.Fail();
                return;
            }
            if (!int.TryParse(tokenVersionClaim, out int tokenVersion))
            {
                AddHeaderSafe(http, "auth", "InvalidToken");
                context.Fail();
                return;
            }
            var dbUser = await _userManager.FindByIdAsync(userId);
            if (dbUser == null || dbUser.IsDeleted == true)
            {
                AddHeaderSafe(http, "auth", "UserNotFound");
                context.Fail();
                return;
            }

            if (dbUser.TokenVersion != tokenVersion)
            {
                AddHeaderSafe(http, "auth", "InvalidToken");
                context.Fail();
                return;
            }

            context.Succeed(requirement);
        }

        private void AddHeaderSafe(HttpContext http, string key, string value)
        {
            if (!http.Response.Headers.ContainsKey(key))
                http.Response.Headers.Add(key, value);
            else
                http.Response.Headers[key] = value;
        }
    }
}
