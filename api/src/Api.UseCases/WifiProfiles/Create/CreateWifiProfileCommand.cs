using Api.UseCases.WifiProfiles.DTOs;

namespace Api.UseCases.WifiProfiles.Create;

public record CreateWifiProfileCommand(
  string Name,
  string Ssid,
  string Password,
  string SecurityType,
  string ForegroundColor,
  string BackgroundColor,
  string? CustomText
) : ICommand<Result<WifiProfileDto>>;
