using Api.UseCases.Interfaces;

namespace Api.UseCases.Auth.ChangeUserRole;

public class ChangeUserRoleHandler : ICommandHandler<ChangeUserRoleCommand, Result>
{
  private readonly IIdentityService _identityService;

  public ChangeUserRoleHandler(IIdentityService identityService)
  {
    _identityService = identityService;
  }

  public async ValueTask<Result> Handle(ChangeUserRoleCommand cmd, CancellationToken ct)
    => await _identityService.ChangeUserRoleAsync(cmd.UserId, cmd.Role);
}
