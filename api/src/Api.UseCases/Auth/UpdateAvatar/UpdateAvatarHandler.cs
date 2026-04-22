using Api.UseCases.Interfaces;

namespace Api.UseCases.Auth.UpdateAvatar;

public class UpdateAvatarHandler(IIdentityService identityService)
  : ICommandHandler<UpdateAvatarCommand, Result<string>>
{
  public async ValueTask<Result<string>> Handle(UpdateAvatarCommand cmd, CancellationToken ct)
  {
    return await identityService.UpdateAvatarAsync(cmd.UserId, cmd.AvatarUrl);
  }
}
