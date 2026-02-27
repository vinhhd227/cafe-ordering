using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate.Specifications;
using Api.Core.Domain.Enums;

namespace Api.UseCases.Orders.Merge;

public class MergeOrdersHandler(IRepositoryBase<Order> repository)
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

    // 4. Merge items from secondaries into primary
    foreach (var secondary in secondaries)
    {
      foreach (var item in secondary.Items)
        primary.AddItemForMerge(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity);

      secondary.CancelAsMerged();
    }

    // 5. Persist
    await repository.UpdateAsync(primary, ct);
    foreach (var secondary in secondaries)
      await repository.UpdateAsync(secondary, ct);

    return Result.Success();
  }
}
