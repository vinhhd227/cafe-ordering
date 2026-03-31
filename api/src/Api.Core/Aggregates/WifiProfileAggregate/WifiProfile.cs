namespace Api.Core.Aggregates.WifiProfileAggregate;

public class WifiProfile : SoftDeletableEntity<int>, IAggregateRoot
{
  private WifiProfile() { }

  public string Name { get; private set; } = string.Empty;
  public string Ssid { get; private set; } = string.Empty;
  public string Password { get; private set; } = string.Empty;
  public string SecurityType { get; private set; } = "WPA";
  public string ForegroundColor { get; private set; } = "#000000";
  public string BackgroundColor { get; private set; } = "#ffffff";
  public string? CustomText { get; private set; }

  public static WifiProfile Create(
    string name,
    string ssid,
    string password,
    string securityType,
    string foregroundColor,
    string backgroundColor,
    string? customText)
  {
    return new WifiProfile
    {
      Name            = Guard.Against.NullOrWhiteSpace(name),
      Ssid            = Guard.Against.NullOrWhiteSpace(ssid),
      Password        = password,
      SecurityType    = Guard.Against.NullOrWhiteSpace(securityType),
      ForegroundColor = Guard.Against.NullOrWhiteSpace(foregroundColor),
      BackgroundColor = Guard.Against.NullOrWhiteSpace(backgroundColor),
      CustomText      = customText,
    };
  }

  public void Update(
    string name,
    string ssid,
    string password,
    string securityType,
    string foregroundColor,
    string backgroundColor,
    string? customText)
  {
    Name            = Guard.Against.NullOrWhiteSpace(name);
    Ssid            = Guard.Against.NullOrWhiteSpace(ssid);
    Password        = password;
    SecurityType    = Guard.Against.NullOrWhiteSpace(securityType);
    ForegroundColor = Guard.Against.NullOrWhiteSpace(foregroundColor);
    BackgroundColor = Guard.Against.NullOrWhiteSpace(backgroundColor);
    CustomText      = customText;
  }
}
