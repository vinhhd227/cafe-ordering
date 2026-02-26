using Api.UseCases.Interfaces;

namespace Api.UseCases.Auth.GetUser;

public class GetUserHandler(IIdentityService identityService)
  : IQueryHandler<GetUserQuery, Result<UserDto>>
{
  public async ValueTask<Result<UserDto>> Handle(GetUserQuery query, CancellationToken ct)
    => await identityService.GetUserByIdAsync(query.UserId);
}
