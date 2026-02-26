using Api.UseCases.Interfaces;

namespace Api.UseCases.Auth.CreateStaffAccount;

public class CreateStaffAccountHandler : ICommandHandler<CreateStaffAccountCommand, Result<TemporaryPasswordDto>>
{
  private readonly IIdentityService _identityService;

  public CreateStaffAccountHandler(IIdentityService identityService)
  {
    _identityService = identityService;
  }

  public async ValueTask<Result<TemporaryPasswordDto>> Handle(CreateStaffAccountCommand cmd, CancellationToken ct)
    => await _identityService.CreateStaffAccountAsync(cmd.Username, cmd.FullName, cmd.Role);
}
