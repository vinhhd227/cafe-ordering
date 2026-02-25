using Api.UseCases.Interfaces;

namespace Api.UseCases.Auth.ChangePassword;

public class ChangePasswordHandler : ICommandHandler<ChangePasswordCommand, Result>
{
  private readonly IIdentityService _identityService;
  private readonly ICurrentUserService _currentUserService;

  public ChangePasswordHandler(
    IIdentityService identityService,
    ICurrentUserService currentUserService)
  {
    _identityService = identityService;
    _currentUserService = currentUserService;
  }

  public async ValueTask<Result> Handle(ChangePasswordCommand cmd, CancellationToken ct)
  {
    var userIdString = _currentUserService.UserId;
    if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out var userId))
      return Result.Unauthorized();

    return await _identityService.ChangePasswordAsync(userId, cmd.CurrentPassword, cmd.NewPassword);
  }
}
