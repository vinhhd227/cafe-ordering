using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Api.Infrastructure.Identity;

/// <summary>
/// Seeds identity data (roles, role permissions, and initial admin user) into AppIdentityDbContext.
/// </summary>
public static class IdentitySeedData
{
  // Permissions per role seeded as role claims (type = "permission")
  private static readonly Dictionary<string, string[]> RolePermissions = new()
  {
    ["Admin"] =
    [
      "admin.access",
      "menu.read",
      "order.create", "order.createManual", "order.read", "order.update", "order.delete",
      "product.create", "product.read", "product.update", "product.delete",
      "staff.create", "staff.read", "staff.update", "staff.deactivate",
      "table.create", "table.read", "table.update",
      "zone.create", "zone.read", "zone.update", "zone.delete",
      "expense.read", "expense.create", "expense.update", "expense.delete",
      "promotion.create", "promotion.read", "promotion.update", "promotion.delete",
      "notification.config",
      "report.read",
      "utility.read", "utility.create", "utility.update", "utility.delete",
      "printer.read", "printer.create", "printer.update", "printer.delete",
      "order.print"
    ],
    ["Staff"] =
    [
      "admin.access",
      "menu.read",
      "order.createManual", "order.read", "order.update",
      "order.print",
      "product.read",
      "table.read", "table.update",
      "zone.read",
      "expense.read", "expense.create", "expense.update",
      "utility.read", "utility.create", "utility.update", "utility.delete",
      "printer.read"
    ],
    ["Customer"] =
    [
      "customer.access",
      "menu.read",
      "order.create", "order.read"
    ]
  };

  public static async Task SeedAsync(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IConfiguration configuration,
    ILogger logger)
  {
    // 1. Seed Roles + Permissions
    foreach (var (roleName, permissions) in RolePermissions)
    {
      var role = await roleManager.FindByNameAsync(roleName);

      if (role is null)
      {
        role = ApplicationRole.Create(roleName, $"{roleName} role");
        var createResult = await roleManager.CreateAsync(role);

        if (createResult.Succeeded)
          logger.LogInformation("Created role: {Role}", roleName);
        else
        {
          logger.LogError("Failed to create role {Role}: {Errors}", roleName,
            string.Join(", ", createResult.Errors.Select(e => e.Description)));
          continue;
        }
      }

      // Seed permissions as role claims (idempotent)
      var existingClaims = await roleManager.GetClaimsAsync(role);
      foreach (var permission in permissions)
      {
        var hasPermission = existingClaims.Any(c => c.Type == "permission" && c.Value == permission);
        if (!hasPermission)
          await roleManager.AddClaimAsync(role, new Claim("permission", permission));
      }
    }

    // 2. Seed dev accounts
    var devAccounts = new[]
    {
      (Username: configuration["AdminAccount:Username"] ?? "admin",
       FullName: configuration["AdminAccount:FullName"] ?? "System Administrator",
       Password: configuration["AdminAccount:Password"] ?? "Ab@123456",
       Role: "Admin"),
      (Username: "staff",  FullName: "Staff Account",    Password: "Ab@123456", Role: "Staff"),
      (Username: "customer", FullName: "Customer Account", Password: "Ab@123456", Role: "Customer"),
    };

    foreach (var (username, fullName, password, role) in devAccounts)
    {
      var user = await userManager.FindByNameAsync(username);
      if (user is not null) continue;

      user = new ApplicationUser
      {
        UserName  = username,
        FullName  = fullName,
        EmailConfirmed = true,
        IsActive  = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      };

      var result = await userManager.CreateAsync(user, password);
      if (result.Succeeded)
      {
        await userManager.AddToRoleAsync(user, role);
        logger.LogInformation("Created {Role} user: {Username}", role, username);
      }
      else
      {
        logger.LogError("Failed to create {Role} user {Username}: {Errors}",
          role, username, string.Join(", ", result.Errors.Select(e => e.Description)));
      }
    }
  }
}
