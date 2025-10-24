using DAL.Entities.AppUser;
using Microsoft.AspNetCore.Identity;

namespace Restaurant.SeedingData
{
    public static class Seeding
    {
        public static async Task DataSeeding (IServiceProvider service)
        {
        var UserManager=service.GetRequiredService<UserManager<ApplicationUser>>();
        var RoleManager=service.GetRequiredService<RoleManager<IdentityRole>>();
        string[] roles = { "Admin", "Customer","Staff","AdminAssistant"};
            foreach (var role in roles)
            {
                if (!await RoleManager.RoleExistsAsync(role))
                {
                    await RoleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }
    }
}
