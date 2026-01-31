using Microsoft.AspNetCore.Identity;

namespace api.Infrastructure.Auth;

public static class IdentitySeeder
{
    public static async Task SeedRolesAsync(RoleManager<IdentityRole<int>> roleManager)
    {
        var roles = new[]
        {
            "Admin",
            "Barber",
            "Customer",
            "Accountant",
            "Manager"
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<int>(role));
            }
        }
    }
}
