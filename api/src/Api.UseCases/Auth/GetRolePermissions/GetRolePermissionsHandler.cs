using Api.UseCases.Common.Interfaces;
using Api.UseCases.Interfaces;

namespace Api.UseCases.Auth.GetRolePermissions;

public class GetRolePermissionsHandler : IQueryHandler<GetRolePermissionsQuery, Result<List<RolePermissionDto>>>
{
  private readonly IIdentityService _identityService;

  public GetRolePermissionsHandler(IIdentityService identityService)
  {
    _identityService = identityService;
  }

  public async ValueTask<Result<List<RolePermissionDto>>> Handle(GetRolePermissionsQuery query, CancellationToken ct)
  {
    return await _identityService.GetRolePermissionsAsync(query.RoleId);
  }
}
