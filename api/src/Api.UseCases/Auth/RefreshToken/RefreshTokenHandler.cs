using Api.UseCases.Interfaces;

namespace Api.UseCases.Auth.RefreshToken;

public class RefreshTokenHandler : ICommandHandler<RefreshTokenCommand, Result<AuthResponseDto>>
{
  private readonly IIdentityService _identityService;

  public RefreshTokenHandler(IIdentityService identityService)
  {
    _identityService = identityService;
  }

  public async ValueTask<Result<AuthResponseDto>> Handle(RefreshTokenCommand cmd, CancellationToken ct)
  {
    return await _identityService.RefreshTokenAsync(cmd.RefreshToken);
  }
}
