using Api.UseCases.Common.Interfaces;
using Api.UseCases.Interfaces;

namespace Api.UseCases.Auth.GetRole;

public class GetRoleHandler : IQueryHandler<GetRoleQuery, Result<RoleDto>>
{
  private readonly IIdentityService _identityService;

  public GetRoleHandler(IIdentityService identityService)
  {
    _identityService = identityService;
  }

  public async ValueTask<Result<RoleDto>> Handle(GetRoleQuery query, CancellationToken ct)
  {
    return await _identityService.GetRoleByIdAsync(query.RoleId);
  }
}
