using Api.UseCases.Common.Interfaces;
using Api.UseCases.Interfaces;

namespace Api.UseCases.Auth.DeleteRole;

public class DeleteRoleHandler : ICommandHandler<DeleteRoleCommand, Result>
{
  private readonly IIdentityService _identityService;

  public DeleteRoleHandler(IIdentityService identityService)
  {
    _identityService = identityService;
  }

  public async ValueTask<Result> Handle(DeleteRoleCommand cmd, CancellationToken ct)
  {
    return await _identityService.DeleteRoleAsync(cmd.RoleId);
  }
}
