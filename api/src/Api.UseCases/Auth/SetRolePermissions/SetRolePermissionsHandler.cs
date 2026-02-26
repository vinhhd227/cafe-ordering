using Api.UseCases.Common.Interfaces;
using Api.UseCases.Interfaces;

namespace Api.UseCases.Auth.SetRolePermissions;

public class SetRolePermissionsHandler : ICommandHandler<SetRolePermissionsCommand, Result>
{
  private readonly IIdentityService _identityService;

  public SetRolePermissionsHandler(IIdentityService identityService)
  {
    _identityService = identityService;
  }

  public async ValueTask<Result> Handle(SetRolePermissionsCommand cmd, CancellationToken ct)
  {
    return await _identityService.SetRolePermissionsAsync(cmd.RoleId, cmd.Permissions);
  }
}
