using Api.Core.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Api.Infrastructure.Data;

public static class IdentitySeedData
{
  public static async Task SeedIdentityAsync(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    ILogger logger)
  {
    // Seed Roles
    var roles = new[] { "Admin", "Manager", "Barista", "Customer" };

    foreach (var roleName in roles)
    {
      if (!await roleManager.RoleExistsAsync(roleName))
      {
        var role = ApplicationRole.Create(roleName, $"{roleName} role");
        var result = await roleManager.CreateAsync(role);

        if (result.Succeeded)
        {
          logger.LogInformation("Created role: {Role}", roleName);
        }
        else
        {
          logger.LogError("Failed to create role {Role}: {Errors}",
            roleName,
            string.Join(", ", result.Errors.Select(e => e.Description)));
        }
      }
    }

    // Seed Admin User
    var adminEmail = "admin@cafeordering.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
      adminUser = ApplicationUser.Create(
        userName: "admin",
        email: adminEmail,
        firstName: "System",
        lastName: "Administrator"
      );

      var result = await userManager.CreateAsync(adminUser, "Admin@123456");

      if (result.Succeeded)
      {
        adminUser.ConfirmEmail();
        await userManager.UpdateAsync(adminUser);
        await userManager.AddToRoleAsync(adminUser, "Admin");
        logger.LogInformation("Created admin user: {Email}", adminEmail);
      }
      else
      {
        logger.LogError("Failed to create admin user: {Errors}",
          string.Join(", ", result.Errors.Select(e => e.Description)));
      }
    }
  }
}
