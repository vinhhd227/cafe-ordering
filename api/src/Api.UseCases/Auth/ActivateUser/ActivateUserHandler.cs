using Api.UseCases.Interfaces;

namespace Api.UseCases.Auth.ActivateUser;

public class ActivateUserHandler : ICommandHandler<ActivateUserCommand, Result>
{
  private readonly IIdentityService _identityService;

  public ActivateUserHandler(IIdentityService identityService)
  {
    _identityService = identityService;
  }

  public async ValueTask<Result> Handle(ActivateUserCommand cmd, CancellationToken ct)
  {
    return await _identityService.ActivateUserAsync(cmd.UserId);
  }
}
