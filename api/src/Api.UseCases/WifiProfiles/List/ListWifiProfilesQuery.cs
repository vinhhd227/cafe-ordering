using Api.UseCases.WifiProfiles.DTOs;

namespace Api.UseCases.WifiProfiles.List;

public record ListWifiProfilesQuery : IQuery<Result<List<WifiProfileDto>>>;
