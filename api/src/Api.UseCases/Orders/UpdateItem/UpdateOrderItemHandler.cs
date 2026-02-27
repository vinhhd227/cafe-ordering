using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate.Specifications;
using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;
using Api.Core.Domain.Enums;
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

    // Session ownership check (customer-facing requests supply SessionId)
    if (request.SessionId.HasValue && order.SessionId != request.SessionId.Value)
      return Result.Forbidden();

    if (!order.Status.CanAddItems)
      return Result.Invalid(new ValidationError("Status", "Only Pending orders can be edited."));

    if (order.PaymentStatus != PaymentStatus.Unpaid)
      return Result.Invalid(new ValidationError("PaymentStatus", "Cannot edit an already paid order."));

    // For non-zero quantity: look up product to get authoritative name + price
    string productName = string.Empty;
    decimal unitPrice  = 0m;

    if (request.Quantity > 0)
    {
      var existing = order.Items.FirstOrDefault(i => i.ProductId == request.ProductId);

      if (existing is not null)
      {
        // Reuse stored name/price for existing items
        productName = existing.ProductName;
        unitPrice   = existing.UnitPrice;
      }
      else
      {
        // New product — fetch from DB
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
    }
    catch (InvalidOperationException ex)
    {
      return Result.Conflict(ex.Message);
    }

    await repository.UpdateAsync(order, ct);

    var dto = new OrderDto(
      order.Id,
      order.OrderNumber,
      order.Status.Name,
      order.PaymentStatus.ToString(),
      order.PaymentMethod.ToString(),
      order.AmountReceived,
      order.TipAmount,
      order.TotalAmount,
      order.OrderDate,
      order.SessionId,
      null, // tableCode — not needed for item edit response
      order.Items.Select(i => new OrderItemDto(
        i.ProductId,
        i.ProductName,
        i.UnitPrice,
        i.Quantity,
        i.TotalPrice
      )).ToList()
    );

    return Result.Success(dto);
  }
}
