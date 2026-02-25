using Api.UseCases.Interfaces;

namespace Api.UseCases.Auth.Login;

public class LoginHandler : ICommandHandler<LoginCommand, Result<AuthResponseDto>>
{
  private readonly IIdentityService _identityService;

  public LoginHandler(IIdentityService identityService)
  {
    _identityService = identityService;
  }

  public async ValueTask<Result<AuthResponseDto>> Handle(LoginCommand cmd, CancellationToken ct)
  {
    return await _identityService.LoginAsync(cmd.Username, cmd.Password);
  }
}
