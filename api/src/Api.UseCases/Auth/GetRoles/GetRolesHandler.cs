using Api.UseCases.Common.Interfaces;
using Api.UseCases.Interfaces;

namespace Api.UseCases.Auth.GetRoles;

public class GetRolesHandler : IQueryHandler<GetRolesQuery, Result<PagedRolesDto>>
{
  private readonly IIdentityService _identityService;

  public GetRolesHandler(IIdentityService identityService)
  {
    _identityService = identityService;
  }

  public async ValueTask<Result<PagedRolesDto>> Handle(GetRolesQuery query, CancellationToken ct)
  {
    return await _identityService.GetRolesAsync(query.Page, query.PageSize, query.Search);
  }
}
