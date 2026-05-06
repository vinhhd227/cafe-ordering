using Api.UseCases.Interfaces;

namespace Api.Web.Endpoints.Auth;

public class LogoutEndpoint(IIdentityService identityService) : EndpointWithoutRequest
{
  public override void Configure()
  {
    Post("/api/auth/logout");
    AllowAnonymous();
    DontAutoTag();
    Description(b => b.WithTags("Authentication"));
  }

  public override async Task HandleAsync(CancellationToken ct)
  {
    var token = HttpContext.Request.Cookies["refreshToken"];

    if (!string.IsNullOrWhiteSpace(token))
    {
      await identityService.RevokeTokenAsync(token);
    }

    HttpContext.Response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/api/auth" });

    await Send.NoContentAsync(ct);
  }
}
