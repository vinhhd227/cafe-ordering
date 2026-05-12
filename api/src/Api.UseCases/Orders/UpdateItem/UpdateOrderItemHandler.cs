using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate.Specifications;
using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;
using Api.UseCases.Orders.DTOs;

namespace Api.UseCases.Orders.UpdateItem;

public class UpdateOrderItemHandler(
  IRepositoryBase<Order> repository,
  IReadRepositoryBase<Product> productRepository)
  : ICommandHandler<UpdateOrderItemCommand, Result<OrderDto>>
{
  public async ValueTask<Result<OrderDto>> Handle(UpdateOrderItemCommand request, CancellationToken ct)
  {
    var spec  = new OrderByIdWithItemsSpec(request.OrderId);
    var order = await repository.FirstOrDefaultAsync(spec, ct);

    if (order is null)
      return Result.NotFound($"Order {request.OrderId} not found.");

    if (request.SessionId.HasValue && order.SessionId != request.SessionId.Value)
      return Result.Forbidden();

    if (!order.Status.CanAddItems)
      return Result.Invalid(new ValidationError("Status", "Only Pending orders can be edited."));

    if (order.PaymentStatus != PaymentStatus.Unpaid)
      return Result.Invalid(new ValidationError("PaymentStatus", "Cannot edit an already paid order."));

    string productName = string.Empty;
    decimal unitPrice  = 0m;

    if (request.Quantity > 0)
    {
      var existing = order.Items.FirstOrDefault(i => i.ProductId == request.ProductId);

      if (existing is not null)
      {
        productName = existing.ProductName;
        unitPrice   = existing.UnitPrice;
      }
      else
      {
        var product = await productRepository.FirstOrDefaultAsync(
          new ProductByIdSpec(request.ProductId), ct);

        if (product is null)
          return Result.NotFound($"Product {request.ProductId} not found.");

        productName = product.Name;
        unitPrice   = product.Price;
      }
    }

    try
    {
      order.SetItemQuantity(request.ProductId, productName, unitPrice, request.Quantity);

      if (request.Quantity == 0)
      {
        order.RemoveFreeGiftItems();
        if (!order.Items.Any())
          order.Cancel();
      }
    }
    catch (InvalidOperationException ex)
    {
      return Result.Conflict(ex.Message);
    }

    await repository.UpdateAsync(order, ct);

    return Result.Success(order.ToOrderDto(null, false));
  }
}
