using Api.Core.Aggregates.GuestSessionAggregate;
using Api.Core.Aggregates.GuestSessionAggregate.Specifications;
using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate.Specifications;

namespace Api.UseCases.Orders.Merge;

public class MergeOrdersHandler(
  IRepositoryBase<Order> repository,
  IReadRepositoryBase<GuestSession> sessionRepository)
  : ICommandHandler<MergeOrdersCommand, Result>
{
  public async ValueTask<Result> Handle(MergeOrdersCommand request, CancellationToken ct)
  {
    if (request.SecondaryOrderIds is null || request.SecondaryOrderIds.Count == 0)
      return Result.Invalid(new ValidationError("SecondaryOrderIds", "At least one secondary order is required."));

    // 1. Load primary
    var primarySpec = new OrderByIdWithItemsSpec(request.PrimaryOrderId);
    var primary = await repository.FirstOrDefaultAsync(primarySpec, ct);

    if (primary is null)
      return Result.NotFound($"Order {request.PrimaryOrderId} not found.");

    if (primary.PaymentStatus != PaymentStatus.Unpaid)
      return Result.Conflict("Primary order must be Unpaid to merge.");

    // 2. Load secondaries
    var secondarySpec = new OrdersByIdsWithItemsSpec(request.SecondaryOrderIds);
    var secondaries = await repository.ListAsync(secondarySpec, ct);

    if (secondaries.Count != request.SecondaryOrderIds.Count)
      return Result.NotFound("One or more secondary orders not found.");

    // 3. Validate each secondary
    foreach (var secondary in secondaries)
    {
      if (secondary.Id == request.PrimaryOrderId)
        return Result.Invalid(new ValidationError("SecondaryOrderIds", "An order cannot be merged with itself."));

      if (secondary.PaymentStatus != PaymentStatus.Unpaid)
        return Result.Conflict($"Order {secondary.OrderNumber} must be Unpaid to merge.");
    }

    // 4. Validate tất cả secondary orders phải thuộc Active session
    // Chặn merge order của khách cũ (session đã Closed) vào bill khách hiện tại,
    // dù khác bàn hay cùng bàn — tránh tính nhầm bill.
    // Merge khác bàn vẫn được phép miễn là session còn Active (nhóm khách ngồi nhiều bàn).
    var allSessionIds = secondaries.Select(o => o.SessionId).Distinct().ToList();
    var sessions = await sessionRepository.ListAsync(new SessionsByIdsSpec(allSessionIds), ct);
    var sessionMap = sessions.ToDictionary(s => s.Id);

    foreach (var secondary in secondaries)
    {
      if (!sessionMap.TryGetValue(secondary.SessionId, out var session))
        return Result.Error($"Session for order {secondary.OrderNumber} not found.");

      if (session.Status == GuestSessionStatus.Closed)
        return Result.Conflict(
          $"Order {secondary.OrderNumber} belongs to a closed session and cannot be merged.");
    }

    // 5. Merge items from secondaries into primary
    foreach (var secondary in secondaries)
    {
      foreach (var item in secondary.Items)
        primary.AddItemForMerge(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity);

      secondary.CancelAsMerged();
    }

    // 6. Persist
    await repository.UpdateAsync(primary, ct);
    foreach (var secondary in secondaries)
      await repository.UpdateAsync(secondary, ct);

    return Result.Success();
  }
}
