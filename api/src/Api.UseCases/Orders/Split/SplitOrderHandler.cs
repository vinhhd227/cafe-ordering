using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate.Specifications;
using Api.Core.Domain.Enums;

namespace Api.UseCases.Orders.Split;

public class SplitOrderHandler(IRepositoryBase<Order> repository)
  : ICommandHandler<SplitOrderCommand, Result<SplitOrderResult>>
{
  public async ValueTask<Result<SplitOrderResult>> Handle(
    SplitOrderCommand request, CancellationToken ct)
  {
    if (request.Items is null || request.Items.Count == 0)
      return Result.Invalid(new ValidationError("Items", "At least one item is required to split."));

    // 1. Load source order
    var spec = new OrderByIdWithItemsSpec(request.OrderId);
    var source = await repository.FirstOrDefaultAsync(spec, ct);

    if (source is null)
      return Result.NotFound($"Order {request.OrderId} not found.");

    if (source.PaymentStatus != PaymentStatus.Unpaid)
      return Result.Conflict("Order must be Unpaid to split.");

    // 2. Validate each split item exists with sufficient quantity
    foreach (var splitItem in request.Items)
    {
      var existing = source.Items.FirstOrDefault(i => i.ProductId == splitItem.ProductId);
      if (existing is null)
        return Result.Invalid(new ValidationError("Items",
          $"Product {splitItem.ProductId} not found in order."));

      if (splitItem.Quantity <= 0)
        return Result.Invalid(new ValidationError("Items",
          $"Split quantity for product {splitItem.ProductId} must be greater than 0."));

      if (splitItem.Quantity > existing.Quantity)
        return Result.Invalid(new ValidationError("Items",
          $"Cannot split {splitItem.Quantity} of product {splitItem.ProductId} — order only has {existing.Quantity}."));
    }

    // 3. Validate source will have at least one item remaining
    var remainingItems = source.Items.Select(i => new
    {
      i.ProductId,
      RemainingQty = i.Quantity - (request.Items.FirstOrDefault(s => s.ProductId == i.ProductId)?.Quantity ?? 0)
    }).Where(i => i.RemainingQty > 0).ToList();

    if (remainingItems.Count == 0)
      return Result.Invalid(new ValidationError("Items",
        "Cannot split all items from an order. At least one item must remain."));

    // 4. Create new order (same session, same device token)
    var newOrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-S";
    var newOrder = Order.Create(source.SessionId, newOrderNumber, source.DeviceToken);
    await repository.AddAsync(newOrder, ct); // get ID first

    // 5. Move items to new order & remove from source
    foreach (var splitItem in request.Items)
    {
      var sourceItem = source.Items.First(i => i.ProductId == splitItem.ProductId);
      newOrder.AddItem(sourceItem.ProductId, sourceItem.ProductName, sourceItem.UnitPrice, splitItem.Quantity);
      source.RemoveItem(splitItem.ProductId, splitItem.Quantity);
    }

    // 6. Persist both
    await repository.UpdateAsync(newOrder, ct);
    await repository.UpdateAsync(source, ct);

    return Result.Success(new SplitOrderResult(newOrder.Id, newOrder.OrderNumber));
  }
}
