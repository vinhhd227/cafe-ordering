using Api.UseCases.Interfaces;

namespace Api.UseCases.Auth.DeactivateUser;

public class DeactivateUserHandler : ICommandHandler<DeactivateUserCommand, Result>
{
  private readonly IIdentityService _identityService;

  public DeactivateUserHandler(IIdentityService identityService)
  {
    _identityService = identityService;
  }

  public async ValueTask<Result> Handle(DeactivateUserCommand cmd, CancellationToken ct)
  {
    return await _identityService.DeactivateUserAsync(cmd.UserId);
  }
}
