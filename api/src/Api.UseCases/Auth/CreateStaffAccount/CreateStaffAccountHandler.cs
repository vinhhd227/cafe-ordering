using Api.UseCases.Interfaces;

namespace Api.UseCases.Auth.CreateStaffAccount;

public class CreateStaffAccountHandler : ICommandHandler<CreateStaffAccountCommand, Result<TemporaryPasswordDto>>
{
  private static readonly HashSet<string> AllowedRoles = ["Admin", "Staff"];

  private readonly IIdentityService _identityService;

  public CreateStaffAccountHandler(IIdentityService identityService)
  {
    _identityService = identityService;
  }

  public async ValueTask<Result<TemporaryPasswordDto>> Handle(CreateStaffAccountCommand cmd, CancellationToken ct)
  {
    if (!AllowedRoles.Contains(cmd.Role))
      return Result<TemporaryPasswordDto>.Invalid(
        new ValidationError(nameof(cmd.Role), $"Role must be one of: {string.Join(", ", AllowedRoles)}"));

    return await _identityService.CreateStaffAccountAsync(cmd.Username, cmd.FullName, cmd.Role);
  }
}
