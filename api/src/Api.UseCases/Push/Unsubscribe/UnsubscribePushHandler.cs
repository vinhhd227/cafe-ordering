using Api.Core.Aggregates.PushSubscriptionAggregate;
using Api.Core.Aggregates.PushSubscriptionAggregate.Specifications;

namespace Api.UseCases.Push.Unsubscribe;

public class UnsubscribePushHandler(IRepositoryBase<PushSubscription> repo)
    : ICommandHandler<UnsubscribePushCommand, Result>
{
    public async ValueTask<Result> Handle(UnsubscribePushCommand cmd, CancellationToken ct)
    {
        var existing = await repo.FirstOrDefaultAsync(
            new PushSubscriptionByEndpointSpec(cmd.Endpoint), ct);

        if (existing is null) return Result.NotFound();

        await repo.DeleteAsync(existing, ct);
        return Result.Success();
    }
}
