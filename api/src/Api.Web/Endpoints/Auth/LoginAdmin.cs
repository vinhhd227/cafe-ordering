using Api.UseCases.Auth.Login;
using Microsoft.AspNetCore.Hosting;

namespace Api.Web.Endpoints.Auth;

public class LoginAdminEndpoint(IMediator mediator, IWebHostEnvironment env) : Ep.Req<LoginRequest>.Res<LoginResponse>
{
    public override void Configure()
    {
        Post("/api/admin/auth/login");
        AllowAnonymous();
        DontAutoTag();
        Description(b => b.WithTags("Authentication"));
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
        {
            await SendAsync(new LoginResponse { Success = false, Message = "Username and password are required" }, 400, ct);
            return;
        }

        var result = await mediator.Send(new LoginCommand(req.Username, req.Password, AppType.Admin, req.RememberMe), ct);

        if (result.Status == Ardalis.Result.ResultStatus.Forbidden)
        {
            await SendAsync(new LoginResponse { Success = false, Message = "Account does not have access to the admin site" }, 403, ct);
            return;
        }

        if (!result.IsSuccess)
        {
            await SendAsync(new LoginResponse { Success = false, Message = "Invalid credentials" }, 401, ct);
            return;
        }

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = !env.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth"
        };
        if (result.Value.RememberMe)
            cookieOptions.MaxAge = TimeSpan.FromDays(30);

        HttpContext.Response.Cookies.Append("refreshToken", result.Value.RefreshToken, cookieOptions);

        await SendOkAsync(new LoginResponse
        {
            Success = true,
            Message = "Login successful",
            AccessToken = result.Value.AccessToken,
            ExpiresAt = result.Value.ExpiresAt
        }, ct);
    }
}
