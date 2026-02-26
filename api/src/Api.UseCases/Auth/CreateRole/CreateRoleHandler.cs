using Api.UseCases.Common.Interfaces;
using Api.UseCases.Interfaces;

namespace Api.UseCases.Auth.CreateRole;

public class CreateRoleHandler : ICommandHandler<CreateRoleCommand, Result>
{
  private readonly IIdentityService _identityService;

  public CreateRoleHandler(IIdentityService identityService)
  {
    _identityService = identityService;
  }

  public async ValueTask<Result> Handle(CreateRoleCommand cmd, CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(cmd.Name))
      return Result.Invalid(new ValidationError(nameof(cmd.Name), "Role name is required."));

    return await _identityService.CreateRoleAsync(cmd.Name.Trim(), cmd.Description?.Trim());
  }
}
