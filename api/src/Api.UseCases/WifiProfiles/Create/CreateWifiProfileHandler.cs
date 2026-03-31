using Api.Core.Aggregates.WifiProfileAggregate;
using Api.UseCases.WifiProfiles.DTOs;

namespace Api.UseCases.WifiProfiles.Create;

public class CreateWifiProfileHandler(IRepositoryBase<WifiProfile> repository)
  : ICommandHandler<CreateWifiProfileCommand, Result<WifiProfileDto>>
{
  public async ValueTask<Result<WifiProfileDto>> Handle(CreateWifiProfileCommand request, CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(request.Name))
      return Result.Invalid(new ValidationError("Name", "Profile name is required."));

    if (string.IsNullOrWhiteSpace(request.Ssid))
      return Result.Invalid(new ValidationError("Ssid", "WiFi SSID is required."));

    var profile = WifiProfile.Create(
      request.Name,
      request.Ssid,
      request.Password,
      request.SecurityType,
      request.ForegroundColor,
      request.BackgroundColor,
      request.CustomText);

    await repository.AddAsync(profile, ct);

    return Result.Success(new WifiProfileDto(
      profile.Id,
      profile.Name,
      profile.Ssid,
      profile.Password,
      profile.SecurityType,
      profile.ForegroundColor,
      profile.BackgroundColor,
      profile.CustomText));
  }
}
