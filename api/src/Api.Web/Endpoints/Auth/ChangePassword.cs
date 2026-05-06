using Api.UseCases.Auth.ChangePassword;
using Api.Web.Extensions;
using FluentValidation;

namespace Api.Web.Endpoints.Auth;

public sealed class ChangePasswordRequest
{
  public string CurrentPassword { get; set; } = string.Empty;
  public string NewPassword { get; set; } = string.Empty;
}

sealed class ChangePasswordRequestValidator : Validator<ChangePasswordRequest>
{
  public ChangePasswordRequestValidator()
  {
    RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Current password is required");
    RuleFor(x => x.NewPassword).NotEmpty().WithMessage("New password is required");
  }
}

public class ChangePasswordEndpoint(IMediator mediator) : Ep.Req<ChangePasswordRequest>.NoRes
{
  public override void Configure()
  {
    Post("/api/auth/change-password");
    // No AllowAnonymous() — FastEndpoints requires auth by default
    DontAutoTag();
    Description(b => b.WithTags("Authentication"));
  }

  public override async Task HandleAsync(ChangePasswordRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new ChangePasswordCommand(req.CurrentPassword, req.NewPassword), ct);
    await this.SendResultAsync(result, ct);
  }
}
