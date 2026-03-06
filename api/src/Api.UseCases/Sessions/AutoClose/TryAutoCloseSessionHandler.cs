using Api.Core.Aggregates.GuestSessionAggregate;
using Api.Core.Aggregates.GuestSessionAggregate.Specifications;
using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate.Specifications;
using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;
using Api.Core.Aggregates.OrderAggregate;

namespace Api.UseCases.Sessions.AutoClose;

public class TryAutoCloseSessionHandler(
  IRepositoryBase<Order> orderRepo,
  IRepositoryBase<GuestSession> sessionRepo,
  IRepositoryBase<Table> tableRepo)
  : ICommandHandler<TryAutoCloseSessionCommand, Result>
{
  public async ValueTask<Result> Handle(TryAutoCloseSessionCommand command, CancellationToken ct)
  {
    var orders = await orderRepo.ListAsync(new OrdersBySessionIdSpec(command.SessionId), ct);

    if (orders.Count == 0)
      return Result.Success();

    var allResolved = orders.All(o =>
      o.Status == OrderStatus.Cancelled ||
      o.PaymentStatus != PaymentStatus.Unpaid);

    if (!allResolved)
      return Result.Success();

    var session = await sessionRepo.FirstOrDefaultAsync(new SessionByIdSpec(command.SessionId), ct);

    if (session is null || session.Status == GuestSessionStatus.Closed)
      return Result.Success();

    session.Close();
    await sessionRepo.UpdateAsync(session, ct);

    if (session.TableId.HasValue)
    {
      var table = await tableRepo.FirstOrDefaultAsync(new TableByIdSpec(session.TableId.Value), ct);
      if (table is not null)
      {
        table.CloseSession();
        await tableRepo.UpdateAsync(table, ct);
      }
    }

    return Result.Success();
  }
}
