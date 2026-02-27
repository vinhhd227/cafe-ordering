using Api.Core.Interfaces;
using Api.UseCases.Interfaces;

namespace Api.UseCases.Auth.ResetUserPassword;

public class ResetUserPasswordHandler(IIdentityService identityService, ICurrentUserService currentUser)
  : ICommandHandler<ResetUserPasswordCommand, Result<TemporaryPasswordDto>>
{
  public async ValueTask<Result<TemporaryPasswordDto>> Handle(ResetUserPasswordCommand cmd, CancellationToken ct)
  {
    if (currentUser.UserId == cmd.UserId.ToString())
      return Result<TemporaryPasswordDto>.Forbidden();

    return await identityService.ResetUserPasswordAsync(cmd.UserId);
  }
}
