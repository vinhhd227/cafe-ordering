using Api.UseCases.Interfaces;

namespace Api.UseCases.Auth.UpdateUser;

public class UpdateUserHandler : ICommandHandler<UpdateUserCommand, Result>
{
  private readonly IIdentityService _identityService;

  public UpdateUserHandler(IIdentityService identityService)
  {
    _identityService = identityService;
  }

  public async ValueTask<Result> Handle(UpdateUserCommand cmd, CancellationToken ct)
  {
    return await _identityService.UpdateUserAsync(cmd.UserId, cmd.FullName, cmd.Email);
  }
}
