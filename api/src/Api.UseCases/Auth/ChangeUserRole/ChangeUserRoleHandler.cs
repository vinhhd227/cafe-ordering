using Api.UseCases.Interfaces;

namespace Api.UseCases.Auth.ChangeUserRole;

public class ChangeUserRoleHandler : ICommandHandler<ChangeUserRoleCommand, Result>
{
  private static readonly HashSet<string> AllowedRoles = ["Admin", "Staff"];

  private readonly IIdentityService _identityService;

  public ChangeUserRoleHandler(IIdentityService identityService)
  {
    _identityService = identityService;
  }

  public async ValueTask<Result> Handle(ChangeUserRoleCommand cmd, CancellationToken ct)
  {
    if (!AllowedRoles.Contains(cmd.Role))
      return Result.Invalid(new ValidationError(
        nameof(cmd.Role),
        $"Role must be one of: {string.Join(", ", AllowedRoles)}"));

    return await _identityService.ChangeUserRoleAsync(cmd.UserId, cmd.Role);
  }
}
