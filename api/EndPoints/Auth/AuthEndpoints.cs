using api.Contracts.Auth;
using api.Domain.Entities;
using api.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace api.EndPoints.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuth(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", async (
            Register.Request request,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<int>> roleManager) =>
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var user = new ApplicationUser
            {
                Email = normalizedEmail,
                UserName = normalizedEmail,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                IsActive = false
            };

            var result = await userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return Results.BadRequest(result.Errors);
            }

            if (await roleManager.RoleExistsAsync("Customer"))
            {
                await userManager.AddToRoleAsync(user, "Customer");
            }

            return Results.Ok(new Register.Response("Registered successfully. Account pending activation."));
        });

        group.MapPost("/login", async (
            Login.Request request,
            UserManager<ApplicationUser> userManager,
            IJwtTokenService tokenService) =>
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var user = await userManager.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail);
            if (user is null)
            {
                return Results.BadRequest("Invalid credentials.");
            }
            
            if (!user.IsActive)
            {
                return Results.Json(
                    new { message = "Account is pending activation. Please contact admin." },
                    statusCode: StatusCodes.Status403Forbidden
                );
            }

            var validPassword = await userManager.CheckPasswordAsync(user, request.Password);
            if (!validPassword)
            {
                return Results.BadRequest("Invalid credentials.");
            }

            var token = await tokenService.CreateTokenAsync(user);
            return Results.Ok(new Login.Response(token.AccessToken, token.ExpiresAt, token.Roles));
        });

        group.MapPost("/check-username", async (
            CheckUsername.Request request,
            UserManager<ApplicationUser> userManager) =>
        {
            var normalized = request.Email.Trim().ToLowerInvariant();
            var exists = await userManager.Users.AnyAsync(x => x.UserName == normalized);
            return Results.Ok(new CheckUsername.Response(exists));
        });

        return app;
    }
}
