using Api.Core.Aggregates.WifiProfileAggregate;
using Api.Core.Aggregates.WifiProfileAggregate.Specifications;
using Api.UseCases.WifiProfiles.DTOs;

namespace Api.UseCases.WifiProfiles.List;

public class ListWifiProfilesHandler(IReadRepositoryBase<WifiProfile> repository)
  : IQueryHandler<ListWifiProfilesQuery, Result<List<WifiProfileDto>>>
{
  public async ValueTask<Result<List<WifiProfileDto>>> Handle(ListWifiProfilesQuery request, CancellationToken ct)
  {
    var profiles = await repository.ListAsync(new AllWifiProfilesSpec(), ct);

    var dtos = profiles.Select(w => new WifiProfileDto(
      w.Id,
      w.Name,
      w.Ssid,
      w.Password,
      w.SecurityType,
      w.ForegroundColor,
      w.BackgroundColor,
      w.CustomText
    )).ToList();

    return Result.Success(dtos);
  }
}
