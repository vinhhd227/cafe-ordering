using Api.UseCases.Common.Interfaces;
using Api.UseCases.Interfaces;

namespace Api.UseCases.Auth.UpdateRole;

public class UpdateRoleHandler : ICommandHandler<UpdateRoleCommand, Result>
{
  private readonly IIdentityService _identityService;

  public UpdateRoleHandler(IIdentityService identityService)
  {
    _identityService = identityService;
  }

  public async ValueTask<Result> Handle(UpdateRoleCommand cmd, CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(cmd.Name))
      return Result.Invalid(new ValidationError(nameof(cmd.Name), "Role name is required."));

    return await _identityService.UpdateRoleAsync(cmd.RoleId, cmd.Name.Trim(), cmd.Description?.Trim());
  }
}
