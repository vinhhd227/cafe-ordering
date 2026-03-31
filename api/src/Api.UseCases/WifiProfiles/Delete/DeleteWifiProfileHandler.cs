using Api.Core.Aggregates.WifiProfileAggregate;
using Api.Core.Aggregates.WifiProfileAggregate.Specifications;

namespace Api.UseCases.WifiProfiles.Delete;

public class DeleteWifiProfileHandler(IRepositoryBase<WifiProfile> repository)
  : ICommandHandler<DeleteWifiProfileCommand, Result>
{
  public async ValueTask<Result> Handle(DeleteWifiProfileCommand request, CancellationToken ct)
  {
    var profile = await repository.FirstOrDefaultAsync(new WifiProfileByIdSpec(request.Id), ct);

    if (profile is null)
      return Result.NotFound($"WiFi profile {request.Id} not found.");

    profile.Delete(request.DeletedBy);
    await repository.UpdateAsync(profile, ct);

    return Result.Success();
  }
}
