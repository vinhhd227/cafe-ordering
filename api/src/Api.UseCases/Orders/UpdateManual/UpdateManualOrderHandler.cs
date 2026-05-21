using Api.Core.Aggregates.GuestSessionAggregate;
using Api.Core.Aggregates.GuestSessionAggregate.Specifications;
using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate.Specifications;
using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;
using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;
using Api.UseCases.Orders.DTOs;

namespace Api.UseCases.Orders.UpdateManual;

public class UpdateManualOrderHandler(
  IRepositoryBase<Order> orderRepository,
  IReadRepositoryBase<GuestSession> sessionRepository,
  IReadRepositoryBase<Table> tableRepository,
  IReadRepositoryBase<Product> productRepository)
  : ICommandHandler<UpdateManualOrderCommand, Result<OrderDto>>
{
  public async ValueTask<Result<OrderDto>> Handle(UpdateManualOrderCommand request, CancellationToken ct)
  {
    var order = await orderRepository.FirstOrDefaultAsync(
      new OrderByIdWithItemsAndPromotionsSpec(request.OrderId), ct);

    if (order is null)
      return Result.NotFound($"Order {request.OrderId} not found.");

    if (request.Items is null || request.Items.Count == 0)
      return Result.Invalid(new ValidationError("Items", "Order must contain at least one item."));

    if (!OrderStatus.TryFromName(request.Status, true, out var status))
      return Result.Invalid(new ValidationError("Status", $"Invalid status: {request.Status}"));

    if (!PaymentStatus.TryFromName(request.PaymentStatus, true, out var paymentStatus))
      return Result.Invalid(new ValidationError("PaymentStatus", $"Invalid payment status: {request.PaymentStatus}"));

    if (!PaymentMethod.TryFromName(request.PaymentMethod, true, out var paymentMethod))
      return Result.Invalid(new ValidationError("PaymentMethod", $"Invalid payment method: {request.PaymentMethod}"));

    var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
    var products = await productRepository.ListAsync(new ProductsByIdsWithVariantGroupsSpec(productIds), ct);

    if (products.Count != productIds.Count)
    {
      var missing = productIds.Except(products.Select(p => p.Id)).First();
      return Result.NotFound($"Product {missing} not found.");
    }

    var productMap = products.ToDictionary(p => p.Id);

    order.UpdateManually(request.OrderedAt, request.GuestCount);

    foreach (var item in request.Items)
    {
      var product = productMap[item.ProductId];

      var pricing = ProductVariantPricingResolver.Resolve(product, item.SelectedVariantValueIds);
      if (pricing is null)
        return Result.Invalid(new ValidationError("SelectedVariantValueIds",
          $"Invalid option value IDs for product '{product.Name}'."));

      order.AddItemManual(item.ProductId, product.Name, pricing.UnitPrice, item.Quantity,
        pricing.Options, item.IsTakeaway, item.Note);
    }

    order.ForceSetStatus(status);
    order.UpdatePayment(paymentStatus, paymentMethod, request.AmountReceived, request.TipAmount);

    await orderRepository.UpdateAsync(order, ct);

    string? tableCode = null;
    bool isManual = false;
    var session = await sessionRepository.FirstOrDefaultAsync(new SessionByIdSpec(order.SessionId), ct);
    if (session is not null)
    {
      isManual = session.Source == GuestSessionSource.Manual;
      if (session.TableId.HasValue)
      {
        var table = await tableRepository.FirstOrDefaultAsync(new TableByIdSpec(session.TableId.Value), ct);
        tableCode = table?.Code;
      }
    }

    return Result.Success(order.ToOrderDto(tableCode, isManual));
  }

}
