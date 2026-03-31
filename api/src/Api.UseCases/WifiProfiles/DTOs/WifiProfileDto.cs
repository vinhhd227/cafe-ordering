namespace Api.UseCases.WifiProfiles.DTOs;

public record WifiProfileDto(
  int Id,
  string Name,
  string Ssid,
  string Password,
  string SecurityType,
  string ForegroundColor,
  string BackgroundColor,
  string? CustomText
);
