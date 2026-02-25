using Api.UseCases.Interfaces;

namespace Api.UseCases.Auth.GetUsers;

public class GetUsersHandler : IQueryHandler<GetUsersQuery, Result<PagedUsersDto>>
{
  private readonly IIdentityService _identityService;

  public GetUsersHandler(IIdentityService identityService)
  {
    _identityService = identityService;
  }

  public async ValueTask<Result<PagedUsersDto>> Handle(GetUsersQuery query, CancellationToken ct)
  {
    return await _identityService.GetUsersAsync(
      query.Page,
      query.PageSize,
      query.Search,
      query.Role,
      query.IsActive);
  }
}
