using Api.Core.Aggregates.PushSubscriptionAggregate;
using Api.Core.Aggregates.PushSubscriptionAggregate.Specifications;

namespace Api.UseCases.Push.Subscribe;

public class SubscribePushHandler(IRepositoryBase<PushSubscription> repo)
    : ICommandHandler<SubscribePushCommand, Result>
{
    public async ValueTask<Result> Handle(SubscribePushCommand cmd, CancellationToken ct)
    {
        // Upsert: nếu endpoint đã tồn tại thì cập nhật keys, ngược lại tạo mới
        var existing = await repo.FirstOrDefaultAsync(
            new PushSubscriptionByEndpointSpec(cmd.Endpoint), ct);

        if (existing is not null)
        {
            existing.Update(cmd.P256dh, cmd.Auth);
            await repo.UpdateAsync(existing, ct);
        }
        else
        {
            var subscription = PushSubscription.Create(cmd.UserId, cmd.Endpoint, cmd.P256dh, cmd.Auth);
            await repo.AddAsync(subscription, ct);
        }

        return Result.Success();
    }
}
